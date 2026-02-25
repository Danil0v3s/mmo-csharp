using Char.Server.Handlers;
using Char.Server.Services;

namespace Char.Server.Tests.Services;

public class ConnectFlowRegressionGuardTests
{
    [Fact]
    public void IsRepeatedConnectPacket_ShouldBeTrue_WhenAccountAlreadyBound()
    {
        Assert.True(ClientConnectHandler.IsRepeatedConnectPacket(200001));
    }

    [Fact]
    public void IsRepeatedConnectPacket_ShouldBeFalse_WhenNoAccountBound()
    {
        Assert.False(ClientConnectHandler.IsRepeatedConnectPacket(null));
    }

    [Fact]
    public void HasDuplicateLiveAccountSession_ShouldBeTrue_WhenAnotherLiveAuthedSessionExists()
    {
        var sessions = new[]
        {
            new ClientConnectHandler.SessionSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), true, 200001, true, true),
            new ClientConnectHandler.SessionSnapshot(Guid.Parse("22222222-2222-2222-2222-222222222222"), true, 200001, true, true)
        };

        var result = ClientConnectHandler.HasDuplicateLiveAccountSession(
            sessions,
            currentSessionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            accountId: 200001);

        Assert.True(result);
    }

    [Fact]
    public void HasDuplicateLiveAccountSession_ShouldBeFalse_WhenOnlyCurrentSessionMatches()
    {
        var sessions = new[]
        {
            new ClientConnectHandler.SessionSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), true, 200001, true, true),
            new ClientConnectHandler.SessionSnapshot(Guid.Parse("22222222-2222-2222-2222-222222222222"), true, 200002, true, true)
        };

        var result = ClientConnectHandler.HasDuplicateLiveAccountSession(
            sessions,
            currentSessionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            accountId: 200001);

        Assert.False(result);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void IsOutOfOrderCharlistRequest_ShouldMatchAuthAndAccountDataPreconditions(
        bool isAuthenticated,
        bool accountDataLoaded,
        bool expectedOutOfOrder)
    {
        var result = CharacterListFlowService.IsOutOfOrderCharlistRequest(isAuthenticated, accountDataLoaded);
        Assert.Equal(expectedOutOfOrder, result);
    }
}
