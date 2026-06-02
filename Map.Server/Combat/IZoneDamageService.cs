using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Zone-specific damage scaling — rAthena <c>battle_calc_gvg_damage</c>
/// (battle.cpp:2121) and <c>battle_calc_bg_damage</c> (battle.cpp:2046),
/// applied as the final stage of <c>battle_calc_attack_gvg_bg</c>
/// (battle.cpp:7225). When the source map carries the GvG (WoE) or
/// Battleground flag, damage is multiplied by a <c>battle_config</c> rate:
///
/// <list type="bullet">
///   <item><b>skills</b> (BF_SKILL) use the per-lane rate —
///         <c>{gvg,bg}_weapon/magic/misc_damage_rate</c>.</item>
///   <item><b>normal attacks</b> use the range rate —
///         <c>{gvg,bg}_short/long_damage_rate</c>.</item>
/// </list>
///
/// rAthena out-of-the-box defaults (battle.cpp battle_data): every rate is 60
/// for the weapon/magic/misc lanes and 80 for short/long. A non-GvG/BG map
/// returns the damage unchanged.
/// </summary>
public interface IZoneDamageService
{
    /// <summary>
    /// Scale <paramref name="damage"/> for the source map's GvG/BG flag, then apply the
    /// PK rate (COMBAT-62). <paramref name="lane"/> selects weapon/magic/misc;
    /// <paramref name="isSkill"/> chooses the per-lane rate (true) vs the short/long range
    /// rate (false); <paramref name="isShortRange"/> picks short vs long for normal attacks.
    /// <paramref name="skillId"/> drives the <c>INF2_IGNOREGVGREDUCTION</c>/
    /// <c>INF2_IGNOREBGREDUCTION</c> bypass (0 for auto-attacks). <paramref name="target"/>
    /// is needed for the PC↔PC PK gate. Returns the damage unchanged on a non-zone map
    /// with PK off; never drops a positive hit below 1.
    /// </summary>
    long Scale(BattleAttackType lane, Entity src, Entity target, long damage,
        bool isSkill, bool isShortRange, ushort skillId);
}
