namespace Map.Server.World;

/// <summary>
/// Cell properties on the map grid. Mirrors rAthena's <c>struct mapcell</c>
/// (map.hpp:776) split into two layers:
///
/// <para><b>Terrain</b> — baked from the .gat file and stored in mapcache.dat
/// (<see cref="Walkable"/>, <see cref="Shootable"/>, <see cref="Water"/>).
/// rAthena's <c>map_gat2cell</c> collapses the .gat numeric type to flags:
/// gat 0/2/4/6 → walkable+shootable; gat 1 → blocked; gat 3 → walkable+water;
/// gat 5 → blocked but shootable. Immutable after load.</para>
///
/// <para><b>Dynamic</b> — set at runtime by NPC registration, skills, etc.
/// (<see cref="NpcTrigger"/>; later: basilica, landprotector, icewall…).
/// MS1 only needs <see cref="NpcTrigger"/> (rAthena <c>CELL_NPC</c>) — set by
/// <c>npc_setcells</c> for every warp + script-NPC trigger box, then checked
/// O(1) per tile step in <c>unit_walktoxy_sub</c>. The rest land with skills
/// in MS3.</para>
/// </summary>
[Flags]
public enum CellFlags : byte
{
    None = 0,

    // Terrain (immutable after load).
    Walkable = 1 << 0,
    Shootable = 1 << 1,
    Water = 1 << 2,

    // Dynamic (runtime mutation via MapData.SetDynamicFlag).
    /// <summary>
    /// rAthena <c>CELL_NPC</c>. Set by warp / OnTouch-script registration;
    /// when a walking player arrives on a cell with this bit, the game looks
    /// up the per-map NPC list to dispatch the trigger (npc.cpp:1924
    /// <c>npc_touch_area_allnpc</c>).
    /// </summary>
    NpcTrigger = 1 << 3,
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
