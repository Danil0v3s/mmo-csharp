using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// End an auction immediately (the seller's buy-now/stop). rAthena <c>clif_parse_Auction_close</c>
/// (clif.cpp, 0x025d) → <c>intif_Auction_close</c>. Completes the sale to the high bidder via
/// <see cref="IAuctionService.BuyNowAsync"/> and replies <c>clif_Auction_close</c> (0 ended / 1 cannot).
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_REQ_MY_SELL_STOP)]
public class AuctionCloseHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionCloseHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_REQ_MY_SELL_STOP>
{
    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_REQ_MY_SELL_STOP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        var ok = await auction.BuyNowAsync(pc, packet.AuctionId);
        client.CloseResult(pc, (short)(ok ? 0 : 1)); // 0 = ended, 1 = cannot end
        logger.LogInformation("Auction close: char {Char} #{Id} (ok={Ok})", pc.CharacterId, packet.AuctionId, ok);
    }
}
