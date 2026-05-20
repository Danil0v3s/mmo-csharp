using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Resolvers;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// First-slice port of rAthena <c>skill_use_id</c> + <c>skill_castend_*</c>
/// chain (skill.cpp). Drives:
/// <list type="number">
///   <item>Lookup the skill in <see cref="ISkillDb"/>.</item>
///   <item>Validate target type / range / sp / cooldown / level.</item>
///   <item>If cast time &gt; 0, schedule the resolution via <see cref="Tick"/>.
///         Otherwise resolve immediately (rAthena instant-cast).</item>
///   <item>On resolve: route through <see cref="ResolveSkill"/> which
///         dispatches by <see cref="SkillDamageKind"/>.</item>
/// </list>
///
/// Per-skill handlers do NOT live in a separate dispatch — for the
/// starter set the resolve switch covers each <see cref="SkillDamageKind"/>;
/// idiosyncratic skills (Pneuma, Storm Gust, etc.) plug in via
/// <see cref="ResolveSkill"/> overrides as they port.
/// </summary>
public sealed class SkillCastService : ISkillCastService
{
    private readonly ISkillDb _db;
    private readonly IEntityRegistry _entities;
    private readonly SkillResolverRegistry _resolvers;
    private readonly IMapFlagService? _mapFlags;
    private readonly IMapWorldRegistry? _maps;
    private readonly IStatusChangeService? _sc;
    private readonly ISkillCastTimingService? _timing;
    // T2.5 — per-skill plugin layer. Consults the registry before
    // falling back to the generic DamageKind dispatch. Optional so
    // the legacy ctor + tests that don't wire it keep working.
    private readonly SkillBehaviorRegistry? _behaviors;
    private readonly IBattleCalculator? _battleCalc;
    private readonly IDamageService? _damage;
    private readonly ILogger<SkillCastService> _logger;

    private readonly List<PendingCast> _pending = new();
    private readonly Dictionary<(EntityId, ushort), long> _cooldowns = new();

    public SkillCastService(
        ISkillDb db,
        IEntityRegistry entities,
        SkillResolverRegistry resolvers,
        ILogger<SkillCastService> logger,
        IMapFlagService? mapFlags = null,
        IMapWorldRegistry? maps = null,
        IStatusChangeService? sc = null,
        ISkillCastTimingService? timing = null,
        SkillBehaviorRegistry? behaviors = null,
        IBattleCalculator? battleCalc = null,
        IDamageService? damage = null)
    {
        _db = db;
        _entities = entities;
        _resolvers = resolvers;
        _mapFlags = mapFlags;
        _maps = maps;
        _sc = sc;
        _timing = timing;
        _behaviors = behaviors;
        _battleCalc = battleCalc;
        _damage = damage;
        _logger = logger;
    }

    /// <summary>
    /// Test-only ctor — wires the five standard resolvers from concrete
    /// services. Keeps the existing test surface working without DI.
    /// </summary>
    public SkillCastService(
        ISkillDb db,
        IEntityRegistry entities,
        IDamageService damage,
        IBattleCalculator battle,
        IStatusChangeService scService,
        ILogger<SkillCastService> logger)
        : this(db, entities,
            new SkillResolverRegistry(new ISkillResolver[]
            {
                new WeaponSkillResolver(battle, damage),
                new MagicSkillResolver(damage),
                new HealSkillResolver(),
                new StatusSkillResolver(scService),
                new MiscSkillResolver(damage),
            }),
            logger)
    {
    }

    public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return SkillCastResult.UnknownSkill;
        if (skillLevel < 1 || skillLevel > def.MaxLevel) return SkillCastResult.LevelOutOfRange;

        // rAthena status_check_skilluse: STONE / FREEZE / STUN / SLEEP /
        // SILENCE / CONFUSION refuse skill casts (status.cpp:1763).
        // Mob and NPC sources bypass — their skill scripts run on engine
        // authority.
        if (source is PlayerEntity && !source.CanCastSkill(_sc))
        {
            return SkillCastResult.CannotAct;
        }

        // rAthena skill.cpp:skill_check_condition_castbegin: `noskill`
        // mapflag refuses all skill casts on this map. Mobs / NPC scripts
        // skip the check — they're authoritative content.
        if (_mapFlags != null && _maps != null && source is PlayerEntity)
        {
            var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == source.MapId);
            if (map != null && _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.NoSkill))
            {
                return SkillCastResult.MapRefused;
            }
        }

        // Players must have learned the skill at the requested level
        // (rAthena pc_checkskill). Mobs / NPCs bypass — they always know
        // their skills, configured via mob_skill_db / npc scripts.
        if (source is PlayerEntity pcSource)
        {
            var learned = pcSource.LearnedSkills.GetValueOrDefault(skillId);
            if (learned < skillLevel) return SkillCastResult.LevelOutOfRange;
        }

        var target = _entities.Get(targetId);
        if (target == null) return SkillCastResult.TargetUnknown;
        if (!IsAlive(target)) return SkillCastResult.TargetDead;
        if (target.MapId != source.MapId) return SkillCastResult.TargetUnknown;

        if (def.Target == SkillTargetMode.TargetEnemy && target is not (MobEntity or PlayerEntity))
            return SkillCastResult.InvalidTargetType;

        // Range — Chebyshev distance same as the auto-attack path.
        var dist = Math.Max(Math.Abs(source.X - target.X), Math.Abs(source.Y - target.Y));
        if (dist > def.Range) return SkillCastResult.OutOfRange;

        // SP cost.
        var spCost = def.SpCost.Length > skillLevel ? def.SpCost[skillLevel] : 0;
        if (source is PlayerEntity pc && pc.Sp < spCost) return SkillCastResult.NotEnoughSp;

        // Cooldown.
        var cdKey = (source.Id, skillId);
        var now = Environment.TickCount64;
        if (_cooldowns.TryGetValue(cdKey, out var readyAt) && readyAt > now) return SkillCastResult.OnCooldown;

        // Consume SP now (rAthena: pre-cast SP deduction).
        if (source is PlayerEntity pc2)
        {
            pc2.Sp -= spCost;
        }

        // Cast time pipeline: pull the raw value from skill_db, then run
        // through the canonical castfix path so DEX scaling + config
        // rates apply (rAthena skill_castfix, skill.cpp:20193). If the
        // timing service isn't wired (test ctor) we fall back to raw.
        var castTime = _timing != null
            ? _timing.CastFix(source, skillId, skillLevel)
            : (def.CastTimeMs.Length > skillLevel ? def.CastTimeMs[skillLevel] : 0);
        if (castTime <= 0)
        {
            ResolveSkill(source, target, skillId, skillLevel);
        }
        else
        {
            _pending.Add(new PendingCast
            {
                Source = source.Id,
                Target = target.Id,
                SkillId = skillId,
                Level = skillLevel,
                ResolveAt = now + castTime,
            });
        }

        // rAthena skill_delayfix (skill.cpp:20456) — the after-cast
        // delay belongs on the unit's CanAct gate, but we approximate
        // by stacking it onto the cooldown floor today. When the
        // PlayerEntity canact_tick lands the delay flows there.
        var cooldown = def.CooldownMs.Length > skillLevel ? def.CooldownMs[skillLevel] : 0;
        var afterDelay = _timing?.DelayFix(source, skillId, skillLevel) ?? 0;
        var lock_ = Math.Max(cooldown, afterDelay);
        if (lock_ > 0) _cooldowns[cdKey] = now + lock_;

        return SkillCastResult.Started;
    }

    public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        if (!IsAlive(target)) return false;

        // T2.5 — per-skill plugin layer first. Returning true means the
        // plugin fully handled the cast; false → fall through to the
        // generic DamageKind dispatch so the plugin can add side
        // effects (proc SC, log telemetry) without re-implementing
        // base damage math.
        if (_behaviors != null && _battleCalc != null && _damage != null)
        {
            var plugin = _behaviors.Get(skillId);
            if (plugin != null)
            {
                var ctx = new Behaviors.SkillBehaviorContext(_entities, _damage, _battleCalc, _sc);
                if (plugin.Resolve(source, target, def, skillLevel, ctx)) return true;
            }
        }

        // Strategy-pattern dispatch — resolvers keyed by DamageKind.
        // New skill kinds add an ISkillResolver class + DI registration;
        // no switch case to edit here.
        var resolver = _resolvers.Get(def.DamageKind);
        resolver?.Resolve(source, target, def, skillLevel);
        return true;
    }

    public void Tick(long nowTick)
    {
        if (_pending.Count == 0) return;

        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var pc = _pending[i];
            if (pc.ResolveAt > nowTick) continue;

            _pending.RemoveAt(i);
            var src = _entities.Get(pc.Source);
            var tgt = _entities.Get(pc.Target);
            if (src != null && tgt != null && IsAlive(src) && IsAlive(tgt))
            {
                ResolveSkill(src, tgt, pc.SkillId, pc.Level);
            }
        }
    }

    // Per-DamageKind resolution lives in ISkillResolver implementations
    // under Skills/Resolvers/. SkillResolverRegistry dispatches.

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,
    };

    private sealed class PendingCast
    {
        public EntityId Source;
        public EntityId Target;
        public ushort SkillId;
        public ushort Level;
        public long ResolveAt;
    }
}
