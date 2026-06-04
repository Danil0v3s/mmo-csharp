using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Vending;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Open a player vending shop from the cart. rAthena <c>clif_parse_OpenVending</c> (clif.cpp, 0x01b2)
/// → <c>vending_openvending</c>. Validates each offer against the vendor's live cart (item present,
/// amount in stock, price ≥ 0), then opens the stall via <see cref="IVendingService.Update"/> (which
/// emits the stall sign + open ack). An empty store name or no valid offers is rejected.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_OPENSTORE2)]
public class OpenStoreHandler(
    IEntityRegistry registry,
    IVendingService vending,
    ILogger<OpenStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_OPENSTORE2>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_OPENSTORE2 packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // rAthena: an invalid (empty) store name, or no cart, aborts the open.
        if (string.IsNullOrEmpty(packet.StoreName)) return Task.CompletedTask;
        if (session.Cart is not { } cart) return Task.CompletedTask;

        // Validate each offer against the live cart (rAthena vending_openvending loop). The wire index
        // is the cart client index (server index + 2).
        var offers = new List<(short index, short qty, int price)>();
        foreach (var o in packet.Offers)
        {
            if (o.Amount <= 0 || o.Price < 0) continue;
            var serverIdx = (short)(o.Index - 2);
            var cartItem = cart.FirstOrDefault(i => i.ServerIndex == serverIdx && i.Amount > 0);
            if (cartItem == null || cartItem.Amount < (uint)o.Amount) continue; // not held / not enough
            offers.Add((serverIdx, o.Amount, o.Price));
        }

        if (offers.Count == 0) return Task.CompletedTask; // nothing valid to sell — don't open

        vending.Update(pc, packet.StoreName, offers);
        logger.LogInformation("OpenStore: char {Char} opened '{Title}' with {N} offer(s)",
            pc.CharacterId, packet.StoreName, offers.Count);
        return Task.CompletedTask;
    }
}
