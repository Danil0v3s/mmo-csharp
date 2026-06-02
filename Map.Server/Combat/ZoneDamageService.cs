using System.Linq;
using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IZoneDamageService"/>. Resolves the source map's GvG /
/// Battleground flag and applies the matching <c>battle_config</c> rate per
/// rAthena <c>battle_calc_gvg_damage</c> / <c>battle_calc_bg_damage</c>
/// (battle.cpp:2121 / 2046). Skills use the per-lane rate; normal attacks use
/// the short/long range rate. Rates come from <see cref="IBattleConfigService"/>
/// (knob names match rAthena's <c>battle_config</c>), defaulting to the
/// out-of-the-box values when a knob is unset.
/// </summary>
public sealed class ZoneDamageService : IZoneDamageService
{
    // rAthena battle.cpp battle_data defaults (lines 11650-11654 / 11866-11870).
    private const int DefaultLaneRate = 60;   // weapon / magic / misc
    private const int DefaultRangeRate = 80;  // short / long

    private readonly IMapFlagService _flags;
    private readonly IMapWorldRegistry _maps;
    private readonly IBattleConfigService? _config;

    public ZoneDamageService(IMapFlagService flags, IMapWorldRegistry maps, IBattleConfigService? config = null)
    {
        _flags = flags;
        _maps = maps;
        _config = config;
    }

    public long Scale(BattleAttackType lane, Entity src, long damage, bool isSkill, bool isShortRange)
    {
        if (damage <= 0) return damage;

        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == src.MapId);
        if (map == null) return damage;

        string zone;
        if (_flags.IsSet(map.Name, MapFlag.Gvg)) zone = "gvg";
        else if (_flags.IsSet(map.Name, MapFlag.Battleground)) zone = "bg";
        else return damage; // no zone scaling on a normal map

        // BF_SKILL → per-lane rate; normal attack → short/long range rate.
        int rate = isSkill
            ? LaneRate(zone, lane)
            : Rate($"{zone}_{(isShortRange ? "short" : "long")}_attack_damage_rate", DefaultRangeRate);

        damage = damage * rate / 100;
        return damage < 1 ? 1 : damage; // rAthena i64max(damage, 1)
    }

    private int LaneRate(string zone, BattleAttackType lane)
    {
        var laneName = lane switch
        {
            BattleAttackType.Magic => "magic",
            BattleAttackType.Misc => "misc",
            _ => "weapon",
        };
        return Rate($"{zone}_{laneName}_attack_damage_rate", DefaultLaneRate);
    }

    private int Rate(string knob, int fallback)
    {
        if (_config == null) return fallback;
        var v = _config.GetValue(knob);
        return v != 0 ? v : fallback;
    }
}
