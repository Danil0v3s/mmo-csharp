using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Renewal port of rAthena's <c>battle_calc_attack</c> chain
/// (battle.cpp:9977). Resolves a single weapon swing — hit-or-miss,
/// critical, element / size / race modifiers, defense reduction —
/// into a <see cref="BattleDamage"/> the auto-attack loop and skill
/// damage handlers can apply via <see cref="IDamageService"/>.
///
/// Magic and misc-attack branches (<c>battle_calc_magic_attack</c>,
/// <c>battle_calc_misc_attack</c>) land alongside the skill system.
/// </summary>
public interface IBattleCalculator
{
    /// <summary>
    /// Resolve a basic (skill_id = 0) weapon attack from <paramref name="source"/>
    /// against <paramref name="target"/>. Mirrors the call shape of
    /// <c>battle_calc_weapon_attack(src, target, 0, 0, BDMG_NONE)</c>.
    /// </summary>
    BattleDamage CalcWeaponAttack(Entity source, Entity target);

    /// <summary>
    /// COMBAT-78 — skill-aware weapon swing. Identical to <see cref="CalcWeaponAttack(Entity,Entity)"/>
    /// except that for a skill (<paramref name="skillId"/> &gt; 0) the <c>crit_atk_rate</c> bump is
    /// NOT baked into the swing (battle.cpp:7787 applies it with the ÷200 skill divisor on the
    /// skill-damage path instead of the auto-attack ÷100). Skill swing builders call this so
    /// <c>ComputeSkillDamage</c> can own the crit_atk_rate ÷200 without double-counting. The default
    /// delegates to the basic swing (test doubles that return a fixed swing are unaffected).
    /// </summary>
    BattleDamage CalcWeaponAttack(Entity source, Entity target, ushort skillId)
        => CalcWeaponAttack(source, target);

    /// <summary>
    /// Wave 67 / Track C — rAthena <c>battle_calc_magic_attack</c>
    /// (battle.cpp:battle_calc_magic_attack). Centralises BF_MAGIC
    /// damage through the same pipeline as weapon attacks:
    ///   base = (MatkMin + MatkMax)/2 + skill_amp
    ///   - apply SC_MAGICPOWER / SC_MOONLITSERENADE caster bumps
    ///   - vs element table (defense element + level)
    ///   - vs MDEF1 (flat) + MDEF2 (% bypass)
    ///   - card_fix with BattleAttackType.Magic
    /// <paramref name="ratePerLevel"/> = skill ratio: the plugin's
    /// <c>CalculateSkillRatio(100,…)</c> when a magic plugin overrides it
    /// (COMBAT-12), else the skill_db DamageRate column.
    /// <paramref name="constantAddition"/> = rAthena ATK_ADD
    /// (<c>battle_calc_skill_constant_addition</c>, battle.cpp:6606), added
    /// after the ratio and before the element/MDEF fix (0 for almost all
    /// renewal magic).
    /// </summary>
    BattleDamage CalcMagicAttack(Entity source, Entity target, ushort skillId, ushort skillLevel, int ratePerLevel, long constantAddition = 0);

    /// <summary>
    /// Wave 67 / Track C — rAthena <c>battle_calc_misc_attack</c>
    /// (battle.cpp:8540). Centralises BF_MISC damage:
    ///   base = src.Level + src.Int
    ///   - element table applied (no def subtract per rAthena)
    ///   - scaled by skill DamageRate
    ///   - card_fix with BattleAttackType.Misc
    /// </summary>
    BattleDamage CalcMiscAttack(Entity source, Entity target, ushort skillId, ushort skillLevel, int ratePerLevel);

    /// <summary>
    /// COMBAT-42 — apply the weapon-SKILL final stage (plant 1-damage clamp +
    /// GvG/BG zone scaling + SC_INVINCIBLE) to a post-ratio weapon-skill damage.
    /// rAthena runs this in <c>battle_calc_weapon_attack</c> (battle.cpp:8000);
    /// the C# weapon-skill funnel computes the raw total, so the skill dispatch
    /// (<c>WeaponSkillImpl.CastendDamageId</c> / <c>SkillAttackService</c>) calls
    /// this before <c>ApplyDamage</c>. <paramref name="isShortRange"/> picks the
    /// melee/ranged plant mode (default true = melee).
    /// </summary>
    /// <para>Default identity so fixed-swing test doubles need no change; the real
    /// <c>BattleCalculator</c> overrides it.</para>
    long ApplyWeaponSkillPlantZone(Entity src, Entity target, long damage, bool isShortRange, ushort skillId = 0) => damage;
}
