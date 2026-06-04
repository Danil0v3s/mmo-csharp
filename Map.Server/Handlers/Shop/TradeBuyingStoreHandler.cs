using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Buying;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Sell items into a buying store. rAthena <c>clif_parse_ReqTradeBuyingStore</c> (clif.cpp, 0x0819) →
/// <c>buyingstore_trade</c>. Converts each line's inventory client index → server index and runs the
/// trade through <see cref="IBuyingStoreService.Trade"/> (which validates against the echoed store id
/// and emits the seller-delete / buyer-update / pickup / result feedback).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_TRADE_BUYING_STORE)]
public class TradeBuyingStoreHandler(
    IEntityRegistry registry,
    IBuyingStoreService buying,
    ILogger<TradeBuyingStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_TRADE_BUYING_STORE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_TRADE_BUYING_STORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // The wire index is the seller's inventory client index (server index + 2).
        var lines = packet.Lines
            .Select(l => ((short)(l.Index - 2), l.Amount))
            .ToList();

        var ok = buying.Trade(pc, packet.BuyerAccountId, packet.StoreId, lines);
        logger.LogInformation("TradeBuyingStore: char {Char} selling into buyer acc {Buyer} (ok={Ok})",
            pc.CharacterId, packet.BuyerAccountId, ok);
        return Task.CompletedTask;
    }
}
