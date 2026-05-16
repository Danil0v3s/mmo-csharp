using Map.Server.Movement;

namespace Map.Server.Tests.Movement;

public class DirectionTests
{
    [Theory]
    [InlineData(Direction.North, 0, 1)]
    [InlineData(Direction.NorthWest, -1, 1)]
    [InlineData(Direction.West, -1, 0)]
    [InlineData(Direction.SouthWest, -1, -1)]
    [InlineData(Direction.South, 0, -1)]
    [InlineData(Direction.SouthEast, 1, -1)]
    [InlineData(Direction.East, 1, 0)]
    [InlineData(Direction.NorthEast, 1, 1)]
    public void DxDy_MatchesRathenaPathTable(Direction dir, int expectedDx, int expectedDy)
    {
        Assert.Equal((short)expectedDx, dir.Dx());
        Assert.Equal((short)expectedDy, dir.Dy());
    }

    [Fact]
    public void Center_HasZeroDelta()
    {
        Assert.Equal((short)0, Direction.Center.Dx());
        Assert.Equal((short)0, Direction.Center.Dy());
    }

    [Theory]
    [InlineData(Direction.NorthWest, true)]
    [InlineData(Direction.SouthEast, true)]
    [InlineData(Direction.NorthEast, true)]
    [InlineData(Direction.SouthWest, true)]
    [InlineData(Direction.North, false)]
    [InlineData(Direction.South, false)]
    [InlineData(Direction.East, false)]
    [InlineData(Direction.West, false)]
    public void IsDiagonal_OnlyTrueForCornerSteps(Direction dir, bool expected)
    {
        Assert.Equal(expected, dir.IsDiagonal());
    }

    [Theory]
    [InlineData(0, 1, Direction.North)]
    [InlineData(-1, 1, Direction.NorthWest)]
    [InlineData(-1, 0, Direction.West)]
    [InlineData(1, 1, Direction.NorthEast)]
    [InlineData(0, 0, Direction.Center)]
    [InlineData(2, 0, Direction.Center)] // out of range → Center sentinel
    public void FromDelta_RoundTripsCardinalAndDiagonals(int dx, int dy, Direction expected)
    {
        Assert.Equal(expected, DirectionExtensions.FromDelta((short)dx, (short)dy));
    }
}
