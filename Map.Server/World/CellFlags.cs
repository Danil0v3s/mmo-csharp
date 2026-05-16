namespace Map.Server.World;

/// <summary>
/// Static cell properties baked from the .gat file and stored in mapcache.dat.
/// Mirrors rAthena's `struct mapcell` (map.cpp:3157 `map_gat2cell`). The .gat
/// numeric type collapses into three boolean flags:
///
///   gat 0 → walkable + shootable
///   gat 1 → blocked (wall)
///   gat 2 → walkable + shootable (synonym for 0)
///   gat 3 → walkable + shootable + water
///   gat 4 → walkable + shootable (synonym for 0)
///   gat 5 → blocked but shootable (gap/cliff — arrows pass over)
///   gat 6 → walkable + shootable (synonym for 0)
///
/// Dynamic cells (ICEWALL, LANDPROTECTOR, BASILICA, etc.) set additional flags
/// at runtime — those come in MS3 with skills. MS1 only needs the static set.
/// </summary>
[Flags]
public enum CellFlags : byte
{
    None = 0,
    Walkable = 1 << 0,
    Shootable = 1 << 1,
    Water = 1 << 2,
}

public static class CellFlagsExtensions
{
    /// <summary>
    /// Map a raw .gat cell type byte (as stored in mapcache.dat after zlib
    /// decompression) to <see cref="CellFlags"/>. Unknown types default to
    /// walkable + shootable (rAthena issues a warning and treats them as 0).
    /// </summary>
    public static CellFlags FromGat(byte gat) => gat switch
    {
        0 => CellFlags.Walkable | CellFlags.Shootable,
        1 => CellFlags.None,
        2 => CellFlags.Walkable | CellFlags.Shootable,
        3 => CellFlags.Walkable | CellFlags.Shootable | CellFlags.Water,
        4 => CellFlags.Walkable | CellFlags.Shootable,
        5 => CellFlags.Shootable,
        6 => CellFlags.Walkable | CellFlags.Shootable,
        _ => CellFlags.Walkable | CellFlags.Shootable,
    };
}
