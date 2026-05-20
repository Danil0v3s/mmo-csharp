using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Zone-specific damage scaling — rAthena
/// <c>battle_calc_gvg_damage</c> (battle.cpp:8265),
/// <c>battle_calc_bg_damage</c> (battle.cpp:8330), and
/// <c>battle_calc_pk_damage</c> (battle.cpp:8390). Each map has a
/// flag (gvg / bg / pk) that scales damage by the corresponding
/// <c>battle_config.<zone>_<range>_damage_rate</c>.
///
/// Per attack-range gates:
/// <list type="bullet">
///   <item><b>short</b> — melee swings.</item>
///   <item><b>long</b> — ranged + skill projectiles.</item>
///   <item><b>weapon</b> — BF_WEAPON.</item>
///   <item><b>magic</b> — BF_MAGIC.</item>
///   <item><b>misc</b> — BF_MISC.</item>
/// </list>
/// rAthena defaults: gvg 25% / 75% / 60% / 60% / 60%, bg 80% / 80%
/// / 80% / 60% / 60%, pk 80% / 70% / 60% / 60% / 60%.
/// </summary>
public interface IZoneDamageService
{
    /// <summary>Scale weapon damage for the source map's zone flag.</summary>
    long ScaleWeapon(Entity src, Entity target, long damage, bool isShortRange);

    /// <summary>Scale magic damage for the source map's zone flag.</summary>
    long ScaleMagic(Entity src, Entity target, long damage, bool isShortRange);

    /// <summary>Scale misc damage for the source map's zone flag.</summary>
    long ScaleMisc(Entity src, Entity target, long damage, bool isShortRange);
}
