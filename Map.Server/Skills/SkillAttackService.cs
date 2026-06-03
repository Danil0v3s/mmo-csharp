using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillAttackService"/>. Wraps the existing
/// <see cref="IBattleCalculator"/> + <see cref="IDamageService"/>
/// pipeline in rAthena's canonical entry-point names so the port of
/// skill.cpp reads 1:1. The per-resolver Resolve methods still own
/// per-skill behavior; this service is the funnel every offensive
/// skill flows through when it actually deals damage.
/// </summary>
public sealed class SkillAttackService : ISkillAttackService
{
    private readonly ISkillDb _db;
    private readonly IBattleCalculator _battle;
    private readonly IDamageService _damage;
    private readonly IEntityRegistry _entities;
    private readonly IStatusChangeService? _sc;
    // COMBAT-31: held lazily to break the DI cycle SkillBehaviorRegistry →
    // SkillImpl (e.g. HolyLight) → SkillAttackService → SkillBehaviorRegistry.
    // The registry is only consulted at skill-cast time (the WeaponSkillImpl
    // plugin lookup), never at construction, so deferring the resolve to first
    // `.Value` makes the constructor graph acyclic. The `_behaviors` property
    // keeps the call site below unchanged.
    private readonly Lazy<Behaviors.SkillBehaviorRegistry>? _behaviorsLazy;
    private Behaviors.SkillBehaviorRegistry? _behaviors => _behaviorsLazy?.Value;
    private readonly ILogger<SkillAttackService> _logger;

    public SkillAttackService(
        ISkillDb db,
        IBattleCalculator battle,
        IDamageService damage,
        IEntityRegistry entities,
        ILogger<SkillAttackService> logger,
        IStatusChangeService? sc = null,
        Lazy<Behaviors.SkillBehaviorRegistry>? behaviors = null)
    {
        _db = db;
        _battle = battle;
        _damage = damage;
        _entities = entities;
        _sc = sc;
        _behaviorsLazy = behaviors;
        _logger = logger;
    }

    public long SkillAttack(BattleAttackType attackType, Entity source, Entity damageSource,
        Entity target, ushort skillId, ushort skillLevel, byte flag = 0)
    {
        if (!IsAlive(target)) return 0;

        var def = _db.Get(skillId);
        long damage = attackType switch
        {
            BattleAttackType.Weapon => WeaponDamage(source, target, skillId, skillLevel, def, flag),
            BattleAttackType.Magic  => CalcMagicDamage(source, target, skillId, skillLevel),
            BattleAttackType.Misc   => CalcMiscDamage(source, target, skillId, skillLevel),
            _ => 0,
        };

        // COMBAT-47 — Safety Wall (melee) / Pneuma (ranged) intercept a weapon SKILL
        // too, not just the auto-attack. BF_LONG when the skill range > 3.
        bool weaponShort = _db.GetRange(skillId) <= 3;
        if (attackType == BattleAttackType.Weapon && _damage.TryGroundUnitBlock(target, weaponShort))
            return 0;

        // COMBAT-49 — SC_BASILICA_CELL sanctuary blocks all attack damage
        // (weapon / magic / misc), unless the skill is SP_SOULEXPLOSION. rAthena
        // battle_calc_damage RENEWAL branch.
        if (skillId != SkillIds.SP_SOULEXPLOSION && _damage.IsBasilicaImmune(target, source))
            return 0;

        // COMBAT-42 — weapon-skill plant/zone is computed post-ratio inside this
        // funnel too (CalcMagic/MiscDamage already ran their own plant/zone stage).
        if (attackType == BattleAttackType.Weapon && damage > 0)
            damage = _battle.ApplyWeaponSkillPlantZone(source, target, damage, isShortRange: weaponShort, skillId: skillId);

        if (damage > 0)
        {
            // rAthena skill.cpp ~3680 — MER_BLESSING / MER_INCAGI on
            // SC_CHANGEUNDEAD targets cap the damage so the target's HP
            // never drops below 1. The mercenary blessing-on-undead semantic
            // is "drain to almost dead" not "kill".
            if (attackType == BattleAttackType.Misc
                && _sc?.Get(target, StatusType.Changeundead) != null)
            {
                var maxAllowed = HpOf(target) - 1;
                if (maxAllowed < 0) maxAllowed = 0;
                if (damage > maxAllowed) damage = maxAllowed;
            }

            if (damage > 0)
            {
                // DamageService still operates in int range. Cap with
                // int.MaxValue — overflows in this slice mean something
                // upstream is wrong, not a real number.
                var clamped = damage > int.MaxValue ? int.MaxValue : (int)damage;
                // COMBAT-17/38 — carry the skill's display hit count so a skill
                // routed through this funnel (rather than the plugin's
                // CastendDamageId) still renders div correctly. Weapon plugins
                // expose the base count via GetMultiHitCount (Sonic Blow → 8) and
                // the per-skill `div_` arm via ModifyDamageData (KN_PIERCE → size+1).
                var hits = 1;
                if (_behaviors?.Get(skillId) is Behaviors.WeaponSkillImpl wsi)
                {
                    var bd = new BattleDamage { Damage = clamped, Hits = wsi.GetMultiHitCount(skillLevel) };
                    wsi.ModifyDamageData(ref bd, source, target, skillLevel);
                    hits = Math.Abs(bd.Hits);
                }
                _damage.ApplyDamage(target, clamped, source, hits);
            }
        }
        return damage;
    }

    private static long HpOf(Entity e) => e switch
    {
        PlayerEntity p => p.Hp,
        MobEntity m => m.Hp,
        _ => long.MaxValue,
    };

    public int SkillAttackArea(Entity source, Entity centerTarget, ushort skillId, ushort skillLevel)
    {
        var splash = _db.GetSplash(skillId, skillLevel);
        if (splash <= 0)
        {
            // Single-target fallback: resolve directly.
            SkillAttack(BattleAttackType.Weapon, source, source, centerTarget, skillId, skillLevel);
            return 1;
        }

        var victims = _entities.ForEachInRange(
            centerTarget.MapId,
            centerTarget.X, centerTarget.Y,
            (short)splash,
            EntityType.Pc | EntityType.Mob);

        var attackType = (BattleAttackType)Math.Max(1, _db.GetType(skillId));
        var hits = 0;
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            if (!IsAlive(v)) continue;
            // Friend/foe gating happens inside SkillAttack via the
            // existing DamageService.ApplyDamage path (which calls
            // its private CanDamage).
            SkillAttack(attackType, source, centerTarget, v, skillId, skillLevel);
            hits++;
        }
        return hits;
    }

    public int SkillAreaSub(Entity center, short range, Func<Entity, bool> onCell)
    {
        var found = _entities.ForEachInRange(
            center.MapId, center.X, center.Y, range,
            EntityType.Pc | EntityType.Mob);
        var count = 0;
        foreach (var e in found)
        {
            if (onCell(e)) count++;
        }
        return count;
    }

    // ---- magic / misc damage helpers ------------------------------
    // Renewal magic damage uses the BattleCalculator MATK formula on
    // the source; misc (BF_MISC) uses the rAthena
    // `battle_calc_misc_attack` shape — fixed level + int scaling, no
    // def/mdef subtract, element table applied through the misc atk
    // element column. The full renewal MATK chain lives in
    // BattleCalculator; this shim handles the common case until that
    // pipe is wired through skill-specific damage rates.

    // Wave 67 / Track C — magic + misc damage now centralised on
    // IBattleCalculator (CalcMagicAttack / CalcMiscAttack). These shims
    // pull the per-level rate from the skill_db and delegate. Keeping
    // the methods here as named entry points avoids touching every
    // call site in SkillAttack above.

    /// <summary>
    /// SKILL-05 — weapon damage for the funnel. When a skill has a registered
    /// <see cref="Behaviors.WeaponSkillImpl"/> plugin, its
    /// <see cref="Behaviors.WeaponSkillImpl.ComputeSkillDamage"/> is the single
    /// ratio authority (same number this skill produces via
    /// ResolveSkill→CastendDamageId) — the <see cref="SkillDefinition.DamageRate"/>
    /// column is consulted ONLY for skills with no plugin.
    /// </summary>
    private long WeaponDamage(Entity source, Entity target, ushort skillId, ushort skillLevel, SkillDefinition? def, byte flag)
    {
        // COMBAT-78 — when a plugin owns the ratio, build the swing skill-aware (crit_atk_rate
        // ÷200 deferred to ComputeSkillDamage); the no-plugin DamageRate fallback keeps the
        // basic auto-attack swing (÷100).
        var plugin = _behaviors?.Get(skillId) as Behaviors.WeaponSkillImpl;
        var swing = _battle.CalcWeaponAttack(source, target, plugin != null ? skillId : (ushort)0);
        if (plugin != null)
            return plugin.ComputeSkillDamage(swing, source, target, skillLevel, ctx: null, miscflag: flag);
        var ratePerLevel = def != null && def.DamageRate.Length > skillLevel ? def.DamageRate[skillLevel] : 100;
        return swing.Total * ratePerLevel / 100;
    }

    private long CalcMagicDamage(Entity source, Entity target, ushort skillId, ushort lvl)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;

        // COMBAT-12: rAthena battle_calc_attack_skill_ratio is keyed on skill_id
        // and shared by BF_WEAPON *and* BF_MAGIC. When a magic plugin overrides
        // CalculateSkillRatio (e.g. MG_SOULSTRIKE's +5*lv vs undead,
        // AL_HOLYLIGHT's +25), that ratio is the authority and REPLACES the
        // skill_db DamageRate column (applied exactly once — no double-count),
        // plus its constant addition (ATK_ADD). Plugins that do NOT override the
        // ratio keep the DamageRate fallback so the ~40 magic plugins relying on
        // it are unaffected.
        int rate;
        long constant = 0;
        var plugin = _behaviors?.Get(skillId);
        if (plugin != null && OverridesMagicRatio(plugin))
        {
            rate = plugin.CalculateSkillRatio(100, source, target, lvl);
            constant = plugin.CalculateSkillConstantAddition(source, target, lvl);
        }
        else
        {
            rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        }
        return _battle.CalcMagicAttack(source, target, skillId, lvl, rate, constant).Damage;
    }

    // COMBAT-12: cache whether a plugin overrides the base (ctx-less)
    // CalculateSkillRatio so CalcMagicDamage only treats a plugin as the ratio
    // authority when it actually defines one — otherwise the DamageRate fallback
    // stands. Reflection runs once per plugin Type.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _magicRatioOverride = new();

    private static bool OverridesMagicRatio(Behaviors.SkillImpl plugin)
        => _magicRatioOverride.GetOrAdd(plugin.GetType(), static t =>
        {
            var m = t.GetMethod(
                nameof(Behaviors.SkillImpl.CalculateSkillRatio),
                new[] { typeof(int), typeof(Entity), typeof(Entity), typeof(ushort) });
            return m != null && m.DeclaringType != typeof(Behaviors.SkillImpl);
        });

    private long CalcMiscDamage(Entity source, Entity target, ushort skillId, ushort lvl)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;
        var ratePerLevel = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        return _battle.CalcMiscAttack(source, target, skillId, lvl, ratePerLevel).Damage;
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,
    };
}
