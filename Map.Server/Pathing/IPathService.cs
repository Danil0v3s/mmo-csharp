namespace Map.Server.Pathing;

/// <summary>
/// Pathing + range helpers. Canonical entry points for rAthena
/// <c>path.cpp</c> (522 lines, 11 functions). The C# port already
/// has a movement service with A* in <see cref="Movement.MovementService"/>;
/// this service exposes the rAthena-named utility helpers so scripts
/// + skills can call <c>path_search</c> / <c>check_distance</c> by
/// name.
/// </summary>
public interface IPathService
{
    /// <summary>rAthena <c>distance</c> — Chebyshev distance (king-move).</summary>
    int Distance(short x0, short y0, short x1, short y1);

    /// <summary>rAthena <c>distance_client</c> — Pythagorean (client view distance).</summary>
    int DistanceClient(short x0, short y0, short x1, short y1);

    /// <summary>rAthena <c>check_distance</c> — Chebyshev within range.</summary>
    bool CheckDistance(short x0, short y0, short x1, short y1, int range);

    /// <summary>rAthena <c>check_distance_client</c>.</summary>
    bool CheckDistanceClient(short x0, short y0, short x1, short y1, int range);

    /// <summary>rAthena <c>direction_diagonal</c>.</summary>
    bool DirectionDiagonal(int dir);

    /// <summary>rAthena <c>direction_opposite</c>.</summary>
    int DirectionOpposite(int dir);

    /// <summary>rAthena <c>path_search</c> — exists a walkable A* path.</summary>
    bool PathSearch(uint mapId, short x0, short y0, short x1, short y1, byte flag);

    /// <summary>rAthena <c>path_search_long</c> — line-of-sight check (no walkable cells required).</summary>
    bool PathSearchLong(uint mapId, short x0, short y0, short x1, short y1);

    /// <summary>rAthena <c>path_blownpos</c> — cell where knockback ends.</summary>
    (short x, short y) BlownPos(uint mapId, short x, short y, int direction, int count);
}
