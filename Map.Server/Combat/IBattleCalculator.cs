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
}
