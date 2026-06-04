using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Request the player's own auctions. rAthena <c>clif_parse_Auction_buysell</c> (clif.cpp, 0x025c) →
/// <c>intif_Auction_requestlist</c> with type = <c>packet.type + 6</c> (6 = my selling, 7 = my buying).
/// Renders the same <c>clif_Auction_results</c> packet.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_REQ_MY_INFO)]
public class AuctionReqMyInfoHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionReqMyInfoHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_REQ_MY_INFO>
{
    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_REQ_MY_INFO packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        session.AuctionStageIndex = -1;
        session.AuctionStageAmount = 0;

        var type = packet.Type + 6; // 6 = my selling, 7 = my buying
        var resp = await auction.RequestListAsync(pc, type, 0, string.Empty, 1);
        var entries = resp is { Success: true }
            ? AuctionResultMapper.ToEntries(resp.Auctions)
            : new List<Core.Server.Packets.Out.ZC.AuctionResultEntry>();
        client.SendResults(pc, resp?.Pages ?? 0, entries);
        logger.LogDebug("Auction my-info: char {Char} type {Type} → {N}", pc.CharacterId, type, entries.Count);
    }
}
