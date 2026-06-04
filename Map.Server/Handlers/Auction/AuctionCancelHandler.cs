using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Cancel an auction listing. rAthena <c>clif_parse_Auction_cancel</c> (clif.cpp, 0x024e) →
/// <c>intif_Auction_cancel</c>. Success → <c>clif_Auction_message(2)</c> (the char side returns the
/// item by mail); failure → char-server-error message.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_CANCEL)]
public class AuctionCancelHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionCancelHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_CANCEL>
{
    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_CANCEL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        var ok = await auction.CancelAsync(pc, packet.AuctionId);
        client.Message(pc, ok ? AuctionResultMessage.Cancelled : AuctionResultMessage.CharServerError);
        logger.LogInformation("Auction cancel: char {Char} #{Id} (ok={Ok})", pc.CharacterId, packet.AuctionId, ok);
    }
}
