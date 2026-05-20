using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Central skill-damage helper. Canonical entry point for rAthena
/// <c>skill_attack</c> (skill.cpp:3561) + the AoE iteration helpers
/// <c>skill_attack_area</c> (skill.cpp:20992) and
/// <c>skill_area_sub</c> (skill.cpp:4188).
///
/// rAthena's <c>skill_attack</c> is the funnel through which every
/// offensive skill resolves: it computes damage via
/// <c>battle_calc_attack</c>, runs the `additional_effect` chain on
/// the attacker, the `counter_additional_effect` chain on the target,
/// applies the damage delay window, and ships the packet. The C#
/// port already has <c>BattleCalculator</c> + <c>DamageService</c>
/// covering the math + delivery — this service is the named entry
/// point that ties them together.
/// </summary>
public interface ISkillAttackService
{
    /// <summary>
    /// rAthena <c>skill_attack</c> — single-target skill damage. Returns
    /// the damage dealt (already applied via <see cref="Combat.IDamageService"/>).
    /// </summary>
    long SkillAttack(BattleAttackType attackType, Entity source, Entity damageSource,
        Entity target, ushort skillId, ushort skillLevel, byte flag = 0);

    /// <summary>
    /// rAthena <c>skill_attack_area</c> — iterate every cell in the
    /// skill's splash radius and apply <see cref="SkillAttack"/> to
    /// each victim.
    /// </summary>
    int SkillAttackArea(Entity source, Entity centerTarget, ushort skillId, ushort skillLevel);

    /// <summary>
    /// rAthena <c>skill_area_sub</c> — predicate iteration helper.
    /// Walks a square cell range and invokes <paramref name="onCell"/>
    /// for each occupant the skill targets. Returns the number of
    /// cells acted upon.
    /// </summary>
    int SkillAreaSub(Entity center, short range, Func<Entity, bool> onCell);
}

