using Map.Server.World;

namespace Map.Server.Tests.World;

public class MapDataTests
{
    [Fact]
    public void Constructor_RejectsZeroDimensions()
    {
        Assert.Throws<ArgumentException>(() => new MapData("x", 0, 10, new byte[0]));
        Assert.Throws<ArgumentException>(() => new MapData("x", 10, 0, new byte[0]));
    }

    [Fact]
    public void Constructor_RejectsMismatchedCellBuffer()
    {
        // 5×5 expects 25 cells; passing 24 is invalid.
        Assert.Throws<ArgumentException>(() => new MapData("x", 5, 5, new byte[24]));
    }

    [Fact]
    public void GetCell_OutOfBounds_ReturnsNone()
    {
        var map = new MapData("x", 3, 3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        Assert.Equal(CellFlags.None, map.GetCell(-1, 0));
        Assert.Equal(CellFlags.None, map.GetCell(0, -1));
        Assert.Equal(CellFlags.None, map.GetCell(3, 0));
        Assert.Equal(CellFlags.None, map.GetCell(0, 3));
        Assert.False(map.IsWalkable(-1, 0));
        Assert.False(map.IsWalkable(3, 3));
    }

    [Fact]
    public void GetCell_InBounds_ReturnsExpectedFlags()
    {
        // 3×2 map: row 0 = walkable, walkable, blocked; row 1 = walkable, water, walkable
        var cells = new byte[] { 0, 0, 1, 0, 3, 0 };
        var map = new MapData("x", 3, 2, cells);

        Assert.True(map.IsWalkable(0, 0));
        Assert.True(map.IsWalkable(1, 0));
        Assert.False(map.IsWalkable(2, 0));
        Assert.True(map.IsWalkable(0, 1));
        Assert.True(map.IsWalkable(1, 1));
        Assert.True(map.IsWater(1, 1));
        Assert.False(map.IsWater(0, 0));
    }

    [Fact]
    public void CellCount_MatchesArea()
    {
        var map = new MapData("x", 12, 7, new byte[12 * 7]);
        Assert.Equal(84, map.CellCount);
    }

    [Fact]
    public void SetDynamicFlag_NpcTrigger_RoundTrips()
    {
        var map = new MapData("x", 5, 5, new byte[25]); // all walkable
        Assert.False(map.HasNpcTrigger(2, 2));
        map.SetDynamicFlag(2, 2, CellFlags.NpcTrigger, true);
        Assert.True(map.HasNpcTrigger(2, 2));
        Assert.True(map.IsWalkable(2, 2)); // terrain unchanged
        // GetCell returns the union of terrain + dynamic flags.
        var flags = map.GetCell(2, 2);
        Assert.True((flags & CellFlags.Walkable) != 0);
        Assert.True((flags & CellFlags.NpcTrigger) != 0);

        map.SetDynamicFlag(2, 2, CellFlags.NpcTrigger, false);
        Assert.False(map.HasNpcTrigger(2, 2));
    }

    [Fact]
    public void SetDynamicFlag_OutOfBounds_IsNoOp()
    {
        var map = new MapData("x", 3, 3, new byte[9]);
        map.SetDynamicFlag(-1, 0, CellFlags.NpcTrigger, true);
        map.SetDynamicFlag(3, 0, CellFlags.NpcTrigger, true);
        map.SetDynamicFlag(0, 3, CellFlags.NpcTrigger, true);
        // No throw; in-bounds cell remains clean.
        Assert.False(map.HasNpcTrigger(0, 0));
    }

    [Fact]
    public void SetDynamicFlag_RejectsTerrainBits()
    {
        var map = new MapData("x", 3, 3, new byte[9]);
        Assert.Throws<ArgumentException>(() =>
            map.SetDynamicFlag(0, 0, CellFlags.Walkable, true));
        Assert.Throws<ArgumentException>(() =>
            map.SetDynamicFlag(0, 0, CellFlags.Shootable, true));
        Assert.Throws<ArgumentException>(() =>
            map.SetDynamicFlag(0, 0, CellFlags.Water, true));
    }
}
