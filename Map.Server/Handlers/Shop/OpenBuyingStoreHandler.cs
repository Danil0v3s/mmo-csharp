using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Buying;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Open a buying store. rAthena <c>clif_parse_ReqOpenBuyingStore</c> (clif.cpp, 0x0811) →
/// <c>buyingstore_create</c>. Forwards the zeny limit + offers (name id / amount / price) to
/// <see cref="IBuyingStoreService.Update"/>, which escrows the zeny and emits the owner list + store
/// sign (or the open-failure).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_OPEN_BUYING_STORE)]
public class OpenBuyingStoreHandler(
    IEntityRegistry registry,
    IBuyingStoreService buying,
    ILogger<OpenBuyingStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_OPEN_BUYING_STORE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_OPEN_BUYING_STORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var offers = packet.Offers
            .Where(o => o.Amount > 0 && o.Price >= 0)
            .Select(o => ((int)o.NameId, o.Amount, o.Price))
            .ToList();

        var ok = buying.Update(pc, packet.StoreName, packet.ZenyLimit, offers);
        logger.LogInformation("OpenBuyingStore: char {Char} '{Title}' limit {Limit} with {N} offer(s) (ok={Ok})",
            pc.CharacterId, packet.StoreName, packet.ZenyLimit, offers.Count, ok);
        return Task.CompletedTask;
    }
}
