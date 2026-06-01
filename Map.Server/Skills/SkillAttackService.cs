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
                _damage.ApplyDamage(target, clamped, source);
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
        var swing = _battle.CalcWeaponAttack(source, target);
        if (_behaviors?.Get(skillId) is Behaviors.WeaponSkillImpl plugin)
            return plugin.ComputeSkillDamage(swing, source, target, skillLevel, ctx: null, miscflag: flag);
        var ratePerLevel = def != null && def.DamageRate.Length > skillLevel ? def.DamageRate[skillLevel] : 100;
        return swing.Total * ratePerLevel / 100;
    }

    private long CalcMagicDamage(Entity source, Entity target, ushort skillId, ushort lvl)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;
        var ratePerLevel = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        return _battle.CalcMagicAttack(source, target, skillId, lvl, ratePerLevel).Damage;
    }

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
