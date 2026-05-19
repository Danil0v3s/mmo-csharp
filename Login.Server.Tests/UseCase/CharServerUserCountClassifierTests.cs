using Login.Server;
using Login.Server.UseCase;

namespace Login.Server.Tests.UseCase;

/// <summary>
/// Mirrors the rAthena <c>login_get_usercount</c> table (login.cpp:484)
/// for PACKETVER ≥ 20170726. Pins the green/yellow/red/purple/hidden
/// mapping so a future config refactor can't silently shift thresholds.
/// </summary>
public class CharServerUserCountClassifierTests
{
    private static LoginServerConfiguration Defaults() => new()
    {
        UserCountDisable = false,
        UserCountLow = 100,
        UserCountMedium = 500,
        UserCountHigh = 1000,
    };

    [Theory]
    [InlineData(0, 0)]    // empty server → green
    [InlineData(100, 0)]  // at low boundary → green
    [InlineData(101, 1)]  // just over low → yellow
    [InlineData(500, 1)]  // at medium boundary → yellow
    [InlineData(501, 2)]  // just over medium → red
    [InlineData(1000, 2)] // at high boundary → red
    [InlineData(1001, 3)] // over high → purple
    [InlineData(9999, 3)] // very crowded → purple
    public void Classify_ReturnsExpectedStatusCode(int users, ushort expected)
    {
        Assert.Equal(expected, CharServerUserCountClassifier.Classify(users, Defaults()));
    }

    [Fact]
    public void Classify_WhenDisabled_AlwaysReturns4()
    {
        var cfg = Defaults();
        cfg.UserCountDisable = true;
        Assert.Equal(4, CharServerUserCountClassifier.Classify(0, cfg));
        Assert.Equal(4, CharServerUserCountClassifier.Classify(500, cfg));
        Assert.Equal(4, CharServerUserCountClassifier.Classify(9999, cfg));
    }
}
