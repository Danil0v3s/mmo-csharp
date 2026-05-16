namespace Map.Server.World;

/// <summary>
/// In-memory representation of a single map's static cell grid. Built once at
/// startup from mapcache.dat; read-only thereafter for MS1.
///
/// Coordinates are short to match the wire protocol. Cells are stored in a
/// flat row-major byte array; index = y * Xs + x.
/// </summary>
public sealed class MapData
{
    private readonly byte[] _cells;

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
    }

    /// <summary>
    /// Read cell flags at (x, y). Returns <see cref="CellFlags.None"/> for
    /// out-of-bounds coordinates (matches rAthena's `map_getcell` boundary
    /// behavior — anything outside the map is "blocked").
    /// </summary>
    public CellFlags GetCell(short x, short y)
    {
        if ((uint)x >= (uint)Xs || (uint)y >= (uint)Ys)
        {
            return CellFlags.None;
        }
        return CellFlagsExtensions.FromGat(_cells[y * Xs + x]);
    }

    public bool IsWalkable(short x, short y) => (GetCell(x, y) & CellFlags.Walkable) != 0;
    public bool IsShootable(short x, short y) => (GetCell(x, y) & CellFlags.Shootable) != 0;
    public bool IsWater(short x, short y) => (GetCell(x, y) & CellFlags.Water) != 0;

    public int CellCount => _cells.Length;
}
