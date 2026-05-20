using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Damage reflect chain — rAthena <c>battle_calc_return_damage</c>
/// (battle.cpp:10030) computes the reflected portion;
/// <c>battle_do_reflect</c> (battle.cpp:7581) applies it back to
/// the attacker. Sources of reflect:
///
/// <list type="bullet">
///   <item>Auto Guard (SC_AUTOGUARD) — blocks then reflects.</item>
///   <item>Shield Reflect (SC_REFLECTSHIELD) — % melee reflect.</item>
///   <item>Maya Purple Card / Lord Knight spirit — flat reflect.</item>
///   <item>Bonus.short_weapon_damage_return — equip-bonus reflect.</item>
/// </list>
///
/// Until the SC system carries those buff slots, we run the
/// equip-bonus branch only (short_weapon_damage_return on equip
/// aggregator). SC-based reflect lands when SC_REFLECTSHIELD ports.
/// </summary>
public interface IBattleReflectService
{
    /// <summary>
    /// rAthena <c>battle_calc_return_damage</c> — given a hit of
    /// <paramref name="damage"/> from <paramref name="attacker"/>
    /// to <paramref name="defender"/>, return how much should bounce
    /// back. Returns 0 when no reflect applies.
    /// </summary>
    long CalcReturnDamage(Entity defender, Entity attacker, long damage, bool isShortRange);

    /// <summary>
    /// rAthena <c>battle_do_reflect</c> — apply the bounced damage
    /// to the original attacker. No-op when <paramref name="rDamage"/> is 0.
    /// </summary>
    void DoReflect(Entity defender, Entity attacker, long rDamage);
}
