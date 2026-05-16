using Map.Server.World;

namespace Map.Server.Movement;

/// <summary>
/// A* pathfinder over a cell grid. Port of rAthena's <c>path_search</c>
/// (path.cpp). Returns a sequence of <see cref="Direction"/> steps from the
/// start cell to the destination (exclusive of start, inclusive of dest).
///
/// Costs: cardinal step = <see cref="MoveCost"/>=10, diagonal step = 14.
/// Diagonal moves require both cardinal neighbors to be walkable too
/// (rAthena disallows squeezing between corners; matches path.cpp behavior).
///
/// Path length capped at <see cref="MaxWalkPath"/>=32 cells, same as rAthena.
/// </summary>
public static class Pathfinder
{
    public const int MoveCost = 10;
    public const int MoveDiagonalCost = 14;
    public const int MaxWalkPath = 32;

    /// <summary>
    /// Search for a path. Returns an empty list when no path is reachable
    /// within <see cref="MaxWalkPath"/> cells, when the destination is
    /// not walkable, or when start == destination.
    /// </summary>
    public static IReadOnlyList<Direction> Search(MapData map, short x0, short y0, short x1, short y1)
    {
        if (x0 == x1 && y0 == y1) return Array.Empty<Direction>();
        if (!map.IsWalkable(x1, y1)) return Array.Empty<Direction>();
        if (!map.IsWalkable(x0, y0)) return Array.Empty<Direction>();

        // Manhattan distance is admissible (never overestimates) and matches
        // rAthena's heuristic. Early reject if minimum cost exceeds the cap.
        var minSteps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        if (minSteps > MaxWalkPath) return Array.Empty<Direction>();

        var open = new PriorityQueue<(short X, short Y), int>();
        var bestG = new Dictionary<int, int>();      // cellKey → best gCost
        var parent = new Dictionary<int, (short X, short Y, Direction Dir)>();

        var startKey = Key(x0, y0, map.Xs);
        bestG[startKey] = 0;
        open.Enqueue((x0, y0), Heuristic(x0, y0, x1, y1));

        while (open.TryDequeue(out var node, out _))
        {
            if (node.X == x1 && node.Y == y1)
            {
                return Reconstruct(parent, node, map.Xs);
            }

            var nodeKey = Key(node.X, node.Y, map.Xs);
            var gHere = bestG[nodeKey];

            // Compute walkability of cardinal neighbors first (used by diagonal corner test).
            bool wN = node.Y + 1 < map.Ys && map.IsWalkable(node.X, (short)(node.Y + 1));
            bool wS = node.Y - 1 >= 0 && map.IsWalkable(node.X, (short)(node.Y - 1));
            bool wE = node.X + 1 < map.Xs && map.IsWalkable((short)(node.X + 1), node.Y);
            bool wW = node.X - 1 >= 0 && map.IsWalkable((short)(node.X - 1), node.Y);

            // Cardinals
            if (wN) TryRelax(node.X, (short)(node.Y + 1), Direction.North, gHere + MoveCost);
            if (wS) TryRelax(node.X, (short)(node.Y - 1), Direction.South, gHere + MoveCost);
            if (wE) TryRelax((short)(node.X + 1), node.Y, Direction.East, gHere + MoveCost);
            if (wW) TryRelax((short)(node.X - 1), node.Y, Direction.West, gHere + MoveCost);

            // Diagonals — only legal when both cardinal neighbors are walkable
            // (anti-corner-cutting rule, rAthena path.cpp behavior).
            if (wN && wE && map.IsWalkable((short)(node.X + 1), (short)(node.Y + 1)))
                TryRelax((short)(node.X + 1), (short)(node.Y + 1), Direction.NorthEast, gHere + MoveDiagonalCost);
            if (wN && wW && map.IsWalkable((short)(node.X - 1), (short)(node.Y + 1)))
                TryRelax((short)(node.X - 1), (short)(node.Y + 1), Direction.NorthWest, gHere + MoveDiagonalCost);
            if (wS && wE && map.IsWalkable((short)(node.X + 1), (short)(node.Y - 1)))
                TryRelax((short)(node.X + 1), (short)(node.Y - 1), Direction.SouthEast, gHere + MoveDiagonalCost);
            if (wS && wW && map.IsWalkable((short)(node.X - 1), (short)(node.Y - 1)))
                TryRelax((short)(node.X - 1), (short)(node.Y - 1), Direction.SouthWest, gHere + MoveDiagonalCost);

            void TryRelax(short nx, short ny, Direction dir, int newG)
            {
                if (newG > MaxWalkPath * MoveDiagonalCost) return; // hard cap

                var nKey = Key(nx, ny, map.Xs);
                if (bestG.TryGetValue(nKey, out var prevG) && prevG <= newG) return;

                bestG[nKey] = newG;
                parent[nKey] = (node.X, node.Y, dir);
                open.Enqueue((nx, ny), newG + Heuristic(nx, ny, x1, y1));
            }
        }

        return Array.Empty<Direction>();
    }

    /// <summary>
    /// Straight-line walkability test from (x0,y0) to (x1,y1) using Bresenham.
    /// Returns true if every intermediate cell is walkable. Mirrors rAthena's
    /// <c>path_search_long</c> (used for line-of-sight / shoot tests).
    /// </summary>
    public static bool HasStraightLine(MapData map, short x0, short y0, short x1, short y1)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var x = (int)x0;
        var y = (int)y0;

        while (x != x1 || y != y1)
        {
            if (!map.IsWalkable((short)x, (short)y)) return false;
            var e2 = err << 1;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
        return map.IsWalkable(x1, y1);
    }

    private static int Heuristic(short x0, short y0, short x1, short y1)
        => MoveCost * (Math.Abs(x1 - x0) + Math.Abs(y1 - y0));

    private static int Key(short x, short y, short xs) => y * xs + x;

    private static IReadOnlyList<Direction> Reconstruct(
        Dictionary<int, (short X, short Y, Direction Dir)> parent,
        (short X, short Y) goal,
        short xs)
    {
        var steps = new List<Direction>();
        var cur = goal;
        while (parent.TryGetValue(Key(cur.X, cur.Y, xs), out var p))
        {
            steps.Add(p.Dir);
            cur = (p.X, p.Y);
        }
        steps.Reverse();
        return steps.Count > MaxWalkPath ? Array.Empty<Direction>() : steps;
    }
}
