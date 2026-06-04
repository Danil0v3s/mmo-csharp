using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Vending;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Buy items from a vending shop. rAthena <c>clif_parse_PurchaseReq</c> (clif.cpp, 0x0134) →
/// <c>vending_purchasereq</c>. Converts each line's cart client index → server index and runs the trade
/// through <see cref="IVendingService.PurchaseReq"/> (which validates against the buyer's stored
/// <see cref="PlayerEntity.VendedId"/> and emits the result/feedback).
/// </summary>
[PacketHandler(PacketHeader.CZ_PC_PURCHASE_ITEMLIST_FROMMC)]
public class PurchaseFromMcHandler(
    IEntityRegistry registry,
    IVendingService vending,
    ILogger<PurchaseFromMcHandler> logger
) : IPacketHandler<MapSessionData, CZ_PC_PURCHASE_ITEMLIST_FROMMC>
{
    public Task HandleAsync(MapSessionData session, CZ_PC_PURCHASE_ITEMLIST_FROMMC packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // The wire index is the cart client index (server cart index + 2).
        var lines = packet.Lines
            .Select(l => ((short)(l.Index - 2), l.Amount))
            .ToList();

        var ok = vending.PurchaseReq(pc, packet.VendorAccountId, pc.VendedId, lines);
        logger.LogInformation("PurchaseFromMc: char {Char} bought from vendor acc {Vendor} (ok={Ok})",
            pc.CharacterId, packet.VendorAccountId, ok);
        return Task.CompletedTask;
    }
}
