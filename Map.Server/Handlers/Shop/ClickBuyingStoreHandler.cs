using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Buying;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Visitor clicked a buying store to view it. rAthena <c>clif_parse_ReqClickBuyingStore</c> (clif.cpp,
/// 0x0817) → <c>buyingstore_open</c>. Asks <see cref="IBuyingStoreService.VisitorListReq"/> to send the
/// store's offers + escrow limit to the visitor.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CLICK_TO_BUYING_STORE)]
public class ClickBuyingStoreHandler(
    IEntityRegistry registry,
    IBuyingStoreService buying,
    ILogger<ClickBuyingStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CLICK_TO_BUYING_STORE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CLICK_TO_BUYING_STORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        buying.VisitorListReq(pc, packet.BuyerAccountId);
        logger.LogInformation("ClickBuyingStore: char {Char} viewing buyer acc {Buyer}", pc.CharacterId, packet.BuyerAccountId);
        return Task.CompletedTask;
    }
}
