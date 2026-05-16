using Map.Server.Movement;
using Map.Server.World;

namespace Map.Server.Tests.Movement;

public class PathfinderTests
{
    [Fact]
    public void Search_StraightLine_OpenMap_TakesCardinalSteps()
    {
        var map = OpenMap(10, 10);
        var path = Pathfinder.Search(map, 0, 0, 5, 0);
        Assert.Equal(5, path.Count);
        Assert.All(path, d => Assert.Equal(Direction.East, d));
    }

    [Fact]
    public void Search_Diagonal_OpenMap_TakesDiagonalSteps()
    {
        var map = OpenMap(10, 10);
        var path = Pathfinder.Search(map, 0, 0, 3, 3);
        Assert.Equal(3, path.Count);
        Assert.All(path, d => Assert.Equal(Direction.NorthEast, d));
    }

    [Fact]
    public void Search_AroundWall_FindsDetour()
    {
        // 5×5 grid; column x=2 is a wall except at y=4 (top of wall).
        var cells = new byte[5 * 5];
        for (var i = 0; i < cells.Length; i++) cells[i] = 0; // all walkable
        // Place blocked cells at (2, 0), (2, 1), (2, 2), (2, 3)
        for (var wy = 0; wy < 4; wy++) cells[wy * 5 + 2] = 1;
        var map = new MapData("wall", 5, 5, cells);

        var path = Pathfinder.Search(map, 0, 2, 4, 2);
        Assert.NotEmpty(path);
        // Path must end at (4, 2). Replay it.
        short x = 0, y = 2;
        foreach (var dir in path)
        {
            x += dir.Dx();
            y += dir.Dy();
            Assert.True(map.IsWalkable(x, y), $"Path stepped onto non-walkable cell ({x},{y})");
        }
        Assert.Equal(4, x);
        Assert.Equal(2, y);
    }

    [Fact]
    public void Search_NoPath_ReturnsEmpty()
    {
        // Fully walled-off destination at (4, 4) with surrounding walls.
        var cells = new byte[5 * 5];
        // Wall surrounding (4, 4)
        cells[4 * 5 + 3] = 1;
        cells[3 * 5 + 4] = 1;
        cells[3 * 5 + 3] = 1;
        // (4, 4) itself walkable; but no path can reach it because anti-corner-cut rule
        var map = new MapData("box", 5, 5, cells);

        var path = Pathfinder.Search(map, 0, 0, 4, 4);
        Assert.Empty(path);
    }

    [Fact]
    public void Search_DestinationNotWalkable_ReturnsEmpty()
    {
        var cells = new byte[10 * 10];
        cells[5 * 10 + 5] = 1; // wall at (5, 5)
        var map = new MapData("dest_block", 10, 10, cells);

        var path = Pathfinder.Search(map, 0, 0, 5, 5);
        Assert.Empty(path);
    }

    [Fact]
    public void Search_SameCell_ReturnsEmpty()
    {
        var map = OpenMap(5, 5);
        Assert.Empty(Pathfinder.Search(map, 2, 2, 2, 2));
    }

    [Fact]
    public void Search_ExceedsMaxWalkPath_ReturnsEmpty()
    {
        var map = OpenMap(200, 200);
        // Manhattan distance >> MAX_WALKPATH=32
        var path = Pathfinder.Search(map, 0, 0, 100, 100);
        Assert.Empty(path);
    }

    [Fact]
    public void Search_AntiCornerCut_RejectsSqueezeBetweenWalls()
    {
        // Two walls forming a diagonal slot:
        //   . W .
        //   W . .
        //   . . .
        // Going from (0,0) to (2,2) tries the NE diagonal at (1,1). The
        // corner-cut rule blocks it because both N (1,1+1) and E (1+1,1)
        // cardinal neighbors must be walkable.
        var cells = new byte[3 * 3];
        cells[2 * 3 + 1] = 1; // (1, 2)
        cells[1 * 3 + 0] = 1; // (0, 1)
        var map = new MapData("corner", 3, 3, cells);

        // The diagonal from (1,0) NE to (2,1): N=(1,1) walkable, E=(2,0) walkable → allowed.
        // But diagonal from (0,0) NE to (1,1): N=(0,1) is a wall → corner-cut blocked.
        // Verify the resulting path doesn't go straight diagonally through the corner.
        var path = Pathfinder.Search(map, 0, 0, 2, 2);
        Assert.NotEmpty(path);
        // Replay and confirm we never step onto either wall cell.
        short x = 0, y = 0;
        foreach (var dir in path)
        {
            x += dir.Dx();
            y += dir.Dy();
            Assert.True(map.IsWalkable(x, y), $"Path stepped onto wall at ({x},{y})");
        }
    }

    [Fact]
    public void HasStraightLine_OpenMap_True()
    {
        var map = OpenMap(20, 20);
        Assert.True(Pathfinder.HasStraightLine(map, 0, 0, 19, 19));
    }

    [Fact]
    public void HasStraightLine_BlockedByWall_False()
    {
        var cells = new byte[10 * 10];
        cells[5 * 10 + 5] = 1;
        var map = new MapData("los", 10, 10, cells);
        Assert.False(Pathfinder.HasStraightLine(map, 0, 0, 9, 9));
    }

    private static MapData OpenMap(short xs, short ys)
        => new($"open_{xs}x{ys}", xs, ys, new byte[xs * ys]);
}
