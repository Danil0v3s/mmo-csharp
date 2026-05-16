using Char.Server.Services;
using Core.Server.IPC;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Services;

public class InterRoutingTests
{
    // P5 — IsAllowedCharName mirrors rAthena `mapif_parse_NameChangeRequest` validation.

    [Theory]
    [InlineData(0, "anything", "AnyName")]            // no restriction
    [InlineData(0, "", "TestName")]                   // no restriction, empty letters
    [InlineData(1, "abcABC", "abcABC")]               // whitelist match
    [InlineData(2, "xyz", "abc")]                     // blacklist no match
    public void IsAllowedCharName_AcceptsValidNames(int option, string letters, string name)
    {
        Assert.True(CharGrpcService.IsAllowedCharName(name, option, letters));
    }

    [Theory]
    [InlineData(1, "abc", "abZ")]                     // whitelist rejects unlisted char
    [InlineData(2, "xyz", "abx")]                     // blacklist rejects listed char
    public void IsAllowedCharName_RejectsInvalidNames(int option, string letters, string name)
    {
        Assert.False(CharGrpcService.IsAllowedCharName(name, option, letters));
    }

    // P5 — MapServerIpcService graceful degradation when no map servers are connected.

    [Fact]
    public async Task MapServerIpcService_NoMapsConnected_BroadcastSucceedsWithoutError()
    {
        var service = new MapServerIpcService(
            new ServerConnectionService(),
            LoggerFactory.Create(_ => { }).CreateLogger<MapServerIpcService>());

        // Should complete without throwing even though no maps are registered.
        await service.BroadcastAsync(new MapBroadcastNotification { Message = "test" });
        await service.BroadcastItemAsync(new MapItemBroadcastNotification { ItemId = 501, Amount = 1 });
        var delivered = await service.SendWhisperAsync(new MapWhisperNotification { TargetCharacterId = 1, TargetName = "x", SourceName = "y", Message = "hi" });
        await service.SendWhisperToGmAsync(new MapWhisperToGmNotification { SourceName = "x", MinGroupId = 99, Message = "alert" });
        await service.NotifyNameChangeAsync(new MapNameChangeNotification { EntityType = 0, EntityId = 1, NewName = "n" });
        await service.NotifyAddressSyncAsync();

        Assert.False(delivered); // no maps → no delivery
    }
}
