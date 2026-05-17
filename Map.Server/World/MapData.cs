namespace Map.Server.World;

/// <summary>
/// In-memory representation of a single map's cell grid. Built once at startup
/// from mapcache.dat; the terrain layer is immutable thereafter, the dynamic
/// layer is mutated by NPC / skill registration (rAthena <c>struct mapcell</c>
/// split — map.hpp:776).
///
/// Coordinates are short to match the wire protocol. Cells are stored in two
/// parallel flat row-major arrays (index = y * Xs + x):
/// <list type="bullet">
///   <item><c>_cells</c> — raw .gat bytes from mapcache.dat (terrain).</item>
///   <item><c>_dynamic</c> — runtime <see cref="CellFlags"/> bits set by
///     NPC + skill systems (warp triggers, basilica, icewall, …).</item>
/// </list>
/// </summary>
public sealed class MapData
{
    private readonly byte[] _cells;
    private readonly byte[] _dynamic;

    public string Name { get; }
    public short Xs { get; }
    public short Ys { get; }

    public MapData(string name, short xs, short ys, byte[] cells)
    {
        if (xs <= 0 || ys <= 0)
        {
            throw new ArgumentException($"Invalid map size {xs}x{ys} for '{name}'");
        }
        if (cells.Length != xs * ys)
        {
            throw new ArgumentException(
                $"Cell buffer length {cells.Length} does not match {xs}x{ys}={xs * ys} for '{name}'");
        }

        Name = name;
        Xs = xs;
        Ys = ys;
        _cells = cells;
        _dynamic = new byte[cells.Length];
    }

    /// <summary>
    /// Read cell flags at (x, y) — terrain OR'd with any dynamic bits set by
    /// NPC / skill registration. Returns <see cref="CellFlags.None"/> for
    /// out-of-bounds coordinates (matches rAthena's <c>map_getcell</c>
    /// boundary behavior — anything outside the map is "blocked").
    /// </summary>
    public CellFlags GetCell(short x, short y)
    {
        if ((uint)x >= (uint)Xs || (uint)y >= (uint)Ys)
        {
            return CellFlags.None;
        }
        var idx = y * Xs + x;
        return CellFlagsExtensions.FromGat(_cells[idx]) | (CellFlags)_dynamic[idx];
    }

    public bool IsWalkable(short x, short y) => (GetCell(x, y) & CellFlags.Walkable) != 0;
    public bool IsShootable(short x, short y) => (GetCell(x, y) & CellFlags.Shootable) != 0;
    public bool IsWater(short x, short y) => (GetCell(x, y) & CellFlags.Water) != 0;
    public bool HasNpcTrigger(short x, short y) => (GetCell(x, y) & CellFlags.NpcTrigger) != 0;

    /// <summary>
    /// Set or clear a dynamic cell flag. Terrain bits (Walkable / Shootable /
    /// Water) are stored separately and cannot be mutated this way; only the
    /// runtime bits (<see cref="CellFlags.NpcTrigger"/>, future basilica /
    /// icewall / …) belong here. Mirrors rAthena's <c>map_setcell</c>
    /// (map.cpp:2842) which writes the same dynamic flag union.
    ///
    /// Out-of-bounds writes are silently ignored — rAthena's <c>npc_setcells</c>
    /// already bounds-checks via <c>map_getcell</c>, and this matches that
    /// "set is a no-op outside the map" behavior.
    /// </summary>
    public void SetDynamicFlag(short x, short y, CellFlags flag, bool value)
    {
        if ((uint)x >= (uint)Xs || (uint)y >= (uint)Ys) return;
        const CellFlags terrainMask = CellFlags.Walkable | CellFlags.Shootable | CellFlags.Water;
        if ((flag & terrainMask) != 0)
        {
            throw new ArgumentException(
                "Terrain flags (Walkable / Shootable / Water) are immutable — use SetDynamicFlag for runtime bits only",
                nameof(flag));
        }
        var idx = y * Xs + x;
        if (value) _dynamic[idx] |= (byte)flag;
        else _dynamic[idx] &= (byte)~(byte)flag;
    }

    public int CellCount => _cells.Length;
}
