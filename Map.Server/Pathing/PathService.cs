using Microsoft.Extensions.Logging;

namespace Map.Server.Pathing;

/// <summary>
/// Default <see cref="IPathService"/>. Distance + direction helpers
/// are math primitives — real now. PathSearch/PathSearchLong/BlownPos
/// route through the existing movement / cell registry once the
/// map-level helpers expose a public lookup; until then they return
/// true so the gate doesn't block downstream callers.
/// </summary>
public sealed class PathService : IPathService
{
    private readonly ILogger<PathService> _logger;
    public PathService(ILogger<PathService> logger) => _logger = logger;

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

    public bool PathSearch(uint mapId, short x0, short y0, short x1, short y1, byte flag) => true;
    public bool PathSearchLong(uint mapId, short x0, short y0, short x1, short y1) => true;

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
        return ((short)(x + dx * count), (short)(y + dy * count));
    }
}
