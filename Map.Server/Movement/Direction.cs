namespace Map.Server.Movement;

/// <summary>
/// 8-direction enum matching rAthena's <c>enum directions</c> in path.hpp.
///
///   <code>
///        +Y axis (NORTH)
///   NW (1) | N (0) | NE (7)
///   W  (2) | C(-1) | E  (6)   →  +X axis (EAST)
///   SW (3) | S (4) | SE (5)
///   </code>
/// </summary>
public enum Direction : sbyte
{
    Center = -1,
    North = 0,
    NorthWest = 1,
    West = 2,
    SouthWest = 3,
    South = 4,
    SouthEast = 5,
    East = 6,
    NorthEast = 7,
}

public static class DirectionExtensions
{
    // Index = (sbyte)Direction. Center (-1) handled separately by callers.
    private static readonly sbyte[] _dx = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static readonly sbyte[] _dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

    /// <summary>Cell delta on the X axis for one step in this direction.</summary>
    public static short Dx(this Direction d) => d == Direction.Center ? (short)0 : _dx[(int)d];

    /// <summary>Cell delta on the Y axis for one step in this direction.</summary>
    public static short Dy(this Direction d) => d == Direction.Center ? (short)0 : _dy[(int)d];

    /// <summary>True for the four diagonal directions (NW/SW/SE/NE).</summary>
    public static bool IsDiagonal(this Direction d) =>
        d == Direction.NorthWest || d == Direction.SouthWest ||
        d == Direction.SouthEast || d == Direction.NorthEast;

    /// <summary>
    /// Derive the direction needed to step from (fromX, fromY) to an adjacent
    /// (toX, toY). Returns <see cref="Direction.Center"/> if they're the same
    /// cell, or if the delta is &gt;1 in either axis (caller should pathfind).
    /// </summary>
    public static Direction FromDelta(short dx, short dy)
    {
        if (dx == 0 && dy == 0) return Direction.Center;
        if (dx < -1 || dx > 1 || dy < -1 || dy > 1) return Direction.Center;
        // Inverse of the dx/dy tables above.
        return (dx, dy) switch
        {
            (0, 1) => Direction.North,
            (-1, 1) => Direction.NorthWest,
            (-1, 0) => Direction.West,
            (-1, -1) => Direction.SouthWest,
            (0, -1) => Direction.South,
            (1, -1) => Direction.SouthEast,
            (1, 0) => Direction.East,
            (1, 1) => Direction.NorthEast,
            _ => Direction.Center,
        };
    }
}
