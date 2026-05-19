using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
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
    private readonly IDamageService _damage;
    private readonly IBattleCalculator _battle;
    private readonly IStatusChangeService _scService;
    private readonly ILogger<SkillCastService> _logger;

    private readonly List<PendingCast> _pending = new();
    private readonly Dictionary<(EntityId, ushort), long> _cooldowns = new();

    public SkillCastService(
        ISkillDb db,
        IEntityRegistry entities,
        IDamageService damage,
        IBattleCalculator battle,
        IStatusChangeService scService,
        ILogger<SkillCastService> logger)
    {
        _db = db;
        _entities = entities;
        _damage = damage;
        _battle = battle;
        _scService = scService;
        _logger = logger;
    }

    public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return SkillCastResult.UnknownSkill;
        if (skillLevel < 1 || skillLevel > def.MaxLevel) return SkillCastResult.LevelOutOfRange;

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

        var castTime = def.CastTimeMs.Length > skillLevel ? def.CastTimeMs[skillLevel] : 0;
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

        var cooldown = def.CooldownMs.Length > skillLevel ? def.CooldownMs[skillLevel] : 0;
        if (cooldown > 0) _cooldowns[cdKey] = now + cooldown;

        return SkillCastResult.Started;
    }

    public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        if (!IsAlive(target)) return false;

        switch (def.DamageKind)
        {
            case SkillDamageKind.Weapon:
                ResolveWeaponSkill(source, target, def, skillLevel);
                break;
            case SkillDamageKind.Magic:
                ResolveMagicSkill(source, target, def, skillLevel);
                break;
            case SkillDamageKind.Heal:
                ResolveHeal(source, target, def, skillLevel);
                break;
            case SkillDamageKind.None:
                ResolveStatusOnly(source, target, def, skillLevel);
                break;
            case SkillDamageKind.Misc:
                ResolveMisc(source, target, def, skillLevel);
                break;
        }
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

    // ---- resolve sub-paths ----

    private void ResolveWeaponSkill(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        var swing = _battle.CalcWeaponAttack(source, target);
        var rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        var scaled = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        _damage.ApplyDamage(target, scaled, source);
    }

    private void ResolveMagicSkill(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        // MATK rolled in [min, max] — status.cpp:2530 matk_max/_min path
        // already runs at status_calc_pc time.
        var s = source.Stats;
        var matk = s.MatkMax > s.MatkMin
            ? Random.Shared.Next(s.MatkMin, s.MatkMax + 1)
            : s.MatkMin;

        var rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        long dmg = matk * rate / 100;

        // Element table — skill carries the attacker element.
        dmg = dmg * ElementTable.GetRate(def.Element, target.Stats.DefenseElement, target.Stats.ElementLevel) / 100;

        // Renewal magic defense = mdef * (4000+mdef)/(4000+10*mdef) - mdef2.
        // Same SIMPLE branch as weapon defense (battle.cpp:6990).
        var mdef1 = target.Stats.Mdef;
        var mdef2 = target.Stats.Mdef2;
        if (mdef1 == -400) mdef1 = -399;
        dmg = dmg * (4000L + mdef1) / (4000L + 10L * mdef1) - mdef2;
        if (dmg < 1) dmg = 1;

        _damage.ApplyDamage(target, (int)Math.Clamp(dmg, 0, int.MaxValue), source);
    }

    private void ResolveHeal(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        // Renewal: heal = (base_lv + int) / 8 * (4 + lvl*8) — matches
        // skill.cpp skill_calc_heal. We approximate with the per-level
        // EffectAmount table (4 + lvl*8) multiplied by (level + int) / 8.
        var multiplier = def.EffectAmount.Length > lvl ? def.EffectAmount[lvl] : 0;
        var baseAmount = (source.Level + source.Stats.IntStat) / 8;
        var heal = Math.Max(1, baseAmount * multiplier);

        switch (target)
        {
            case PlayerEntity p:
                p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
                break;
            case MobEntity m:
                m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
                break;
        }
        _logger.LogDebug("Heal: {Source} -> {Target} for {Amount}", source.Id.Value, target.Id.Value, heal);
    }

    private void ResolveStatusOnly(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        if (def.StatusType == StatusType.None) return;
        var duration = def.StatusDurationMs.Length > lvl ? def.StatusDurationMs[lvl] : 0;
        var val1 = def.EffectAmount.Length > lvl ? def.EffectAmount[lvl] : lvl;
        _scService.Start(target, def.StatusType, val1, 0, 0, 0, duration, source);
    }

    private void ResolveMisc(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        // Misc skills (pre-computed damage). Placeholder until specific
        // skills (e.g. Soul Strike) port their per-level formulas.
        var amount = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 0;
        _damage.ApplyDamage(target, amount, source);
    }

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
