using Map.Server.Status;

namespace Map.Server.Tests.Status;

public class StatPointTableTests
{
    [Theory]
    [InlineData(1, 2)]    // 1→2 costs 2 + (0)/10 = 2
    [InlineData(9, 2)]    // 9→10 costs 2 + 8/10 = 2
    [InlineData(10, 2)]   // 10→11 costs 2 + 9/10 = 2
    [InlineData(11, 3)]   // 11→12 costs 2 + 10/10 = 3
    [InlineData(20, 3)]   // 20→21 costs 2 + 19/10 = 3
    [InlineData(99, 11)]  // 99→100 costs 2 + 98/10 = 11
    [InlineData(100, 16)] // 100→101 transitions to the second branch: 16 + 0
    [InlineData(105, 20)] // 105→106 costs 16 + 4*1 = 20
    [InlineData(110, 24)] // 110→111 costs 16 + 4*2 = 24
    public void CostToIncrement_MatchesRathenaFormula(int current, int expectedCost)
    {
        Assert.Equal(expectedCost, StatPointTable.CostToIncrement(current));
    }

    [Fact]
    public void TotalCost_Sums5Increments_From1()
    {
        // 1→2 + 2→3 + 3→4 + 4→5 + 5→6 = 2 + 2 + 2 + 2 + 2 = 10
        Assert.Equal(10, StatPointTable.TotalCost(1, 5));
    }

    [Fact]
    public void TotalCost_Bridges100Boundary()
    {
        // 99→100 = 11, 100→101 = 16; total = 27
        Assert.Equal(27, StatPointTable.TotalCost(99, 2));
    }
}
