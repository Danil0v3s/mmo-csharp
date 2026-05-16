using Map.Server.World;

namespace Map.Server.Tests.World;

public class CellFlagsTests
{
    [Theory]
    [InlineData(0, CellFlags.Walkable | CellFlags.Shootable)]
    [InlineData(1, CellFlags.None)]
    [InlineData(2, CellFlags.Walkable | CellFlags.Shootable)]
    [InlineData(3, CellFlags.Walkable | CellFlags.Shootable | CellFlags.Water)]
    [InlineData(4, CellFlags.Walkable | CellFlags.Shootable)]
    [InlineData(5, CellFlags.Shootable)]
    [InlineData(6, CellFlags.Walkable | CellFlags.Shootable)]
    public void FromGat_KnownTypes_MatchRathenaMapping(byte gat, CellFlags expected)
    {
        Assert.Equal(expected, CellFlagsExtensions.FromGat(gat));
    }

    [Theory]
    [InlineData((byte)99)]
    [InlineData((byte)255)]
    public void FromGat_UnknownType_FallsBackToWalkable(byte gat)
    {
        // rAthena's map_gat2cell logs a warning and treats unknown as walkable+shootable.
        Assert.Equal(CellFlags.Walkable | CellFlags.Shootable, CellFlagsExtensions.FromGat(gat));
    }
}
