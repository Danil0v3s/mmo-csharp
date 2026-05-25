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
    private readonly ILogger<SkillAttackService> _logger;

    public SkillAttackService(
        ISkillDb db,
        IBattleCalculator battle,
        IDamageService damage,
        IEntityRegistry entities,
        ILogger<SkillAttackService> logger,
        IStatusChangeService? sc = null)
    {
        _db = db;
        _battle = battle;
        _damage = damage;
        _entities = entities;
        _sc = sc;
        _logger = logger;
    }

    public long SkillAttack(BattleAttackType attackType, Entity source, Entity damageSource,
        Entity target, ushort skillId, ushort skillLevel, byte flag = 0)
    {
        if (!IsAlive(target)) return 0;

        // BattleCalculator covers weapon damage today; magic / misc
        // paths use simpler heuristics in their resolvers but the
        // central entry stays here so the next port iteration plugs
        // them all into the same path.
        // For weapon swings we hit the renewal battle path; skills
        // amplify the result by the skill_db DamageRate column. The
        // dedicated `battle_calc_weapon_attack(skill_id, skill_lv)`
        // overload from rAthena is on the porting backlog — when it
        // lands the multiplier moves there.
        var def = _db.Get(skillId);
        var ratePerLevel = def != null && def.DamageRate.Length > skillLevel
            ? def.DamageRate[skillLevel]
            : 100;
        long damage = attackType switch
        {
            BattleAttackType.Weapon => _battle.CalcWeaponAttack(source, target).Damage * ratePerLevel / 100,
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

    private long CalcMagicDamage(Entity source, Entity target, ushort skillId, ushort lvl)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;
        var ratePerLevel = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        var baseDmg = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        long damage = Math.Max(1, baseDmg * ratePerLevel / 100);

        // rAthena <c>SC_MAGICPOWER</c> (status.cpp:10556-10564) — Sage's
        // Mystic Amplification. The next magic cast deals
        // <c>(100 + 5 * Val1) %</c> of base damage; SC ends on consume.
        // Val1 carries the skill level (1..5).
        if (_sc != null)
        {
            var mp = _sc.Get(source, StatusType.Magicpower);
            if (mp != null && mp.Val1 > 0)
            {
                var bumpPct = 100 + 5 * mp.Val1;
                damage = damage * bumpPct / 100;
                _sc.End(source, StatusType.Magicpower);
            }

            // SC_MOONLITSERENADE — Wanderer / Minstrel song.
            // Val2 stores the % Matk boost for the band.
            var mls = _sc.Get(source, StatusType.Moonlitserenade);
            if (mls != null && mls.Val2 > 0)
            {
                damage += damage * mls.Val2 / 100;
            }
        }
        return damage;
    }

    /// <summary>
    /// rAthena <c>battle_calc_misc_attack</c> (battle.cpp:8540-ish) —
    /// BF_MISC base damage = caster <c>(level + int)</c> scaled by the
    /// skill's <c>damage_rate</c> column. No defense subtract; this
    /// matches the rAthena MISC branch where the only modifiers are
    /// the skill ratio + element table (applied upstream).
    /// </summary>
    private long CalcMiscDamage(Entity source, Entity target, ushort skillId, ushort lvl)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;
        var ratePerLevel = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
        // Per-skill ratio bump (Mercenary blessing / increase agility scale
        // with caster level + INT). rAthena: `dmg = sd ? sd->status.int_ +
        // 10 : src->level * sstatus->int_;` We approximate with the
        // (level + int) sum which works for both mob and PC sources.
        long baseDmg = source.Level + source.Stats.IntStat;
        return Math.Max(1, baseDmg * ratePerLevel / 100);
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,
    };
}
