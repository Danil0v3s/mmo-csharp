using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Bid on an auction. rAthena <c>clif_parse_Auction_bid</c> (clif.cpp, 0x024f) →
/// <c>intif_Auction_bid</c>. Pre-gates affordability (specific not-enough-zeny message), then
/// escrows the bid via <see cref="IAuctionService.BidAsync"/> (the char side mails the prior bidder a
/// refund). Success → confirmation message; failure → bid-fail message.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_BUY)]
public class AuctionBidHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionBidHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_BUY>
{
    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_BUY packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        if (packet.Money <= 0 || session.CharacterData == null || session.CharacterData.Zeny < (uint)packet.Money)
        {
            client.Message(pc, AuctionResultMessage.NotEnoughZenyBid);
            return;
        }

        var ok = await auction.BidAsync(pc, packet.AuctionId, packet.Money);
        client.Message(pc, ok ? AuctionResultMessage.BidSuccess : AuctionResultMessage.BidFail);
        logger.LogInformation("Auction bid: char {Char} bid {Bid}z on #{Id} (ok={Ok})",
            pc.CharacterId, packet.Money, packet.AuctionId, ok);
    }
}
