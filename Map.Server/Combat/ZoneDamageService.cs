using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IZoneDamageService"/>. Looks up the source
/// map's zone flag, then applies the right battle_config rate.
/// Until <c>IBattleConfigService</c> ships (B-L1), rates are
/// pulled from a static defaults table that mirrors rAthena's
/// out-of-the-box values.
/// </summary>
public sealed class ZoneDamageService : IZoneDamageService
{
    /// <summary>rAthena defaults from conf/battle/gvg.conf / bg.conf / pk.conf.</summary>
    private static class Defaults
    {
        // gvg
        public const int GvgWeapon = 60;
        public const int GvgMagic = 60;
        public const int GvgMisc = 60;
        public const int GvgShort = 25;
        public const int GvgLong = 75;
        // bg
        public const int BgWeapon = 80;
        public const int BgMagic = 60;
        public const int BgMisc = 60;
        public const int BgShort = 80;
        public const int BgLong = 80;
        // pk
        public const int PkWeapon = 60;
        public const int PkMagic = 60;
        public const int PkMisc = 60;
        public const int PkShort = 80;
        public const int PkLong = 70;
    }

    private readonly IMapFlagService _flags;
    private readonly IMapWorldRegistry _maps;

    public ZoneDamageService(IMapFlagService flags, IMapWorldRegistry maps)
    {
        _flags = flags;
        _maps = maps;
    }

    public long ScaleWeapon(Entity src, Entity target, long damage, bool isShortRange)
    {
        var (rangeRate, typeRate) = PickRates(src,
            (Defaults.GvgShort, Defaults.GvgLong, Defaults.GvgWeapon),
            (Defaults.BgShort,  Defaults.BgLong,  Defaults.BgWeapon),
            (Defaults.PkShort,  Defaults.PkLong,  Defaults.PkWeapon),
            isShortRange);
        return Apply(damage, rangeRate, typeRate);
    }

    public long ScaleMagic(Entity src, Entity target, long damage, bool isShortRange)
    {
        var (rangeRate, typeRate) = PickRates(src,
            (Defaults.GvgShort, Defaults.GvgLong, Defaults.GvgMagic),
            (Defaults.BgShort,  Defaults.BgLong,  Defaults.BgMagic),
            (Defaults.PkShort,  Defaults.PkLong,  Defaults.PkMagic),
            isShortRange);
        return Apply(damage, rangeRate, typeRate);
    }

    public long ScaleMisc(Entity src, Entity target, long damage, bool isShortRange)
    {
        var (rangeRate, typeRate) = PickRates(src,
            (Defaults.GvgShort, Defaults.GvgLong, Defaults.GvgMisc),
            (Defaults.BgShort,  Defaults.BgLong,  Defaults.BgMisc),
            (Defaults.PkShort,  Defaults.PkLong,  Defaults.PkMisc),
            isShortRange);
        return Apply(damage, rangeRate, typeRate);
    }

    private (int RangeRate, int TypeRate) PickRates(
        Entity src,
        (int Short, int Long, int Type) gvg,
        (int Short, int Long, int Type) bg,
        (int Short, int Long, int Type) pk,
        bool isShortRange)
    {
        // Resolve source map.
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == src.MapId);
        if (map == null) return (100, 100);

        if (_flags.IsSet(map.Name, MapFlag.Gvg))
        {
            return (isShortRange ? gvg.Short : gvg.Long, gvg.Type);
        }
        // BG / PK are tracked the same way once the flags land.
        // For now no BG or PK flag — return 100/100 (no scaling).
        return (100, 100);
    }

    private static long Apply(long damage, int rangeRate, int typeRate)
    {
        if (damage <= 0) return damage;
        // rAthena: damage = damage * range_rate / 100 * type_rate / 100.
        // Done in two steps to match the integer-rounding behavior.
        damage = damage * rangeRate / 100;
        damage = damage * typeRate / 100;
        return damage;
    }
}
