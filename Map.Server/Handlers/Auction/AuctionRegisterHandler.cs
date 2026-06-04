using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Register the staged item for auction. rAthena <c>clif_parse_Auction_register</c> (clif.cpp, 0x024d)
/// → <c>intif_Auction_register</c>. Pre-gates the listing fee (so the not-enough-zeny message is
/// specific), then escrows the staged item + fee via <see cref="IAuctionService.RegisterAsync"/>.
/// Success → <c>clif_Auction_message(1)</c> + clear the stage; failure → cancelled message.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_ADD)]
public class AuctionRegisterHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionRegisterHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_ADD>
{
    private const int FeePerHour = 12000; // rAthena auction_feeperhour

    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_ADD packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        // rAthena gates: a staged item must exist; prices must be ascending; 1..48 hours.
        if (session.AuctionStageIndex < 0 || session.AuctionStageAmount < 1)
        {
            client.Message(pc, AuctionResultMessage.Cancelled);
            return;
        }
        if (packet.NowMoney >= packet.MaxMoney || packet.Hours < 1 || packet.Hours > 48)
        {
            client.Message(pc, AuctionResultMessage.Cancelled);
            return;
        }

        // Specific fee gate (rAthena clif_Auction_message flag 5) before escrow.
        var fee = (long)packet.Hours * FeePerHour;
        if (session.CharacterData == null || session.CharacterData.Zeny < fee)
        {
            client.Message(pc, AuctionResultMessage.NotEnoughZenyFee);
            return;
        }

        var id = await auction.RegisterAsync(pc, session.AuctionStageIndex, session.AuctionStageAmount,
            packet.NowMoney, packet.MaxMoney, packet.Hours);

        if (id > 0)
        {
            session.AuctionStageIndex = -1;
            session.AuctionStageAmount = 0;
            client.Message(pc, AuctionResultMessage.BidSuccess); // rAthena flag 1 = confirmation
            logger.LogInformation("Auction register: char {Char} listed #{Id}", pc.CharacterId, id);
        }
        else
        {
            client.Message(pc, AuctionResultMessage.Cancelled);
        }
    }
}
