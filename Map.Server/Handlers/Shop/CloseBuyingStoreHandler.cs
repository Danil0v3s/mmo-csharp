using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Buying;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Close the player's buying store. rAthena <c>clif_parse_ReqCloseBuyingStore</c> (clif.cpp, 0x0815) →
/// <c>buyingstore_close</c>. Tears down the store (<see cref="IBuyingStoreService.Close"/>), which
/// refunds the unspent escrow and removes the store sign.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CLOSE_BUYING_STORE)]
public class CloseBuyingStoreHandler(
    IEntityRegistry registry,
    IBuyingStoreService buying,
    ILogger<CloseBuyingStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CLOSE_BUYING_STORE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CLOSE_BUYING_STORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        buying.Close(pc);
        logger.LogInformation("CloseBuyingStore: char {Char} closed their buying store", pc.CharacterId);
        return Task.CompletedTask;
    }
}
