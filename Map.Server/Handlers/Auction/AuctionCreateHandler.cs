using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Open the auction window / cancel the register tab. rAthena <c>clif_parse_Auction_cancelreg</c>
/// (clif.cpp, 0x024b). Type 1 clears the staged item; otherwise the window is (re)opened.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_CREATE)]
public class AuctionCreateHandler(
    IEntityRegistry registry,
    IAuctionClientService client,
    ILogger<AuctionCreateHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_CREATE>
{
    public Task HandleAsync(MapSessionData session, CZ_AUCTION_CREATE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // rAthena cancelreg: any value clears the staged item; type 0 also (re)opens the window.
        session.AuctionStageIndex = -1;
        session.AuctionStageAmount = 0;
        if (packet.Type != 1) client.OpenWindow(pc);
        logger.LogDebug("Auction create/reset: char {Char} (type {Type})", pc.CharacterId, packet.Type);
        return Task.CompletedTask;
    }
}
