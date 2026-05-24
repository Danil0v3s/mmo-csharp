using Map.Server.Movement;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Pathing;

/// <summary>
/// Default <see cref="IPathService"/>. Distance / direction primitives
/// + path-search delegations onto the canonical <see cref="Pathfinder"/>
/// A* (in `Map.Server/Movement/Pathfinder.cs`). The `Path*` calls are
/// the rAthena-named entry points; <see cref="Pathfinder.Search"/>
/// is the actual A* (cardinal/diagonal cost 10/14, MAX_WALKPATH=32,
/// Manhattan ×10 heuristic, anti-corner-cut diagonal gate). Resolves
/// <paramref name="mapId"/> via <see cref="IMapWorldRegistry"/>; falls
/// back to "permissive true" when the registry isn't wired (unit tests).
/// </summary>
public sealed class PathService : IPathService
{
    private readonly IMapWorldRegistry? _world;
    private readonly ILogger<PathService> _logger;
    public PathService(ILogger<PathService> logger, IMapWorldRegistry? world = null)
    {
        _logger = logger;
        _world = world;
    }

    public int Distance(short x0, short y0, short x1, short y1)
        => Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));

    public int DistanceClient(short x0, short y0, short x1, short y1)
        => (int)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));

    public bool CheckDistance(short x0, short y0, short x1, short y1, int range)
        => Distance(x0, y0, x1, y1) <= range;

    public bool CheckDistanceClient(short x0, short y0, short x1, short y1, int range)
        => DistanceClient(x0, y0, x1, y1) <= range;

    public bool DirectionDiagonal(int dir) => (dir & 1) == 1; // rAthena DIR_DIAG
    public int DirectionOpposite(int dir) => (dir + 4) % 8;

    /// <summary>
    /// rAthena <c>path_search</c> — true if an A* walkable path
    /// exists from (x0,y0) to (x1,y1) within MAX_WALKPATH cells.
    /// Delegates to <see cref="Pathfinder.Search"/>.
    /// </summary>
    public bool PathSearch(uint mapId, short x0, short y0, short x1, short y1, byte flag)
    {
        var map = ResolveMap(mapId);
        if (map is null) return true; // permissive fallback for tests
        if (x0 == x1 && y0 == y1) return true;
        var path = Pathfinder.Search(map, x0, y0, x1, y1);
        return path.Count > 0;
    }

    /// <summary>
    /// rAthena <c>path_search_long</c> — Bresenham line-of-sight.
    /// Passes through cells that are walkable; no path-length cap.
    /// </summary>
    public bool PathSearchLong(uint mapId, short x0, short y0, short x1, short y1)
    {
        var map = ResolveMap(mapId);
        if (map is null) return true;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx = x0, cy = y0;
        // Iterate at most (dx+dy+1) steps — the chebyshev distance bound.
        var safety = dx + dy + 2;
        while (safety-- > 0)
        {
            if (cx == x1 && cy == y1) return true;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx)  { err += dx; cy += sy; }
            if (cx < 0 || cy < 0 || cx >= map.Xs || cy >= map.Ys) return false;
            if (cx == x1 && cy == y1) return true;
            if (!map.IsWalkable((short)cx, (short)cy)) return false;
        }
        return false;
    }

    public (short x, short y) BlownPos(uint mapId, short x, short y, int direction, int count)
    {
        // rAthena's eight-direction delta table.
        // DIR_N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6, NW=7.
        var (dx, dy) = direction switch
        {
            0 => (0, 1), 1 => (1, 1), 2 => (1, 0), 3 => (1, -1),
            4 => (0, -1), 5 => (-1, -1), 6 => (-1, 0), 7 => (-1, 1),
            _ => (0, 0),
        };
        // rAthena path_blownpos halts at the first non-walkable cell
        // along the slide direction (so a knockback against a wall
        // stops at the wall, not behind it).
        var map = ResolveMap(mapId);
        if (map is null || count <= 0)
        {
            return ((short)(x + dx * count), (short)(y + dy * count));
        }
        short cx = x, cy = y;
        for (var step = 0; step < count; step++)
        {
            var nx = (short)(cx + dx);
            var ny = (short)(cy + dy);
            if (nx < 0 || ny < 0 || nx >= map.Xs || ny >= map.Ys) break;
            if (!map.IsWalkable(nx, ny)) break;
            cx = nx; cy = ny;
        }
        return (cx, cy);
    }

    private MapData? ResolveMap(uint mapId)
    {
        if (_world is null) return null;
        foreach (var m in _world.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m;
        }
        return null;
    }
}
