using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Vending;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Buyer clicked a vending stall to browse it. rAthena <c>clif_parse_VendingListReq</c> (clif.cpp,
/// 0x0130) → <c>vending_vendinglistreq</c>. Asks <see cref="IVendingService.VendingListReq"/> to stamp
/// the viewed vender id on the buyer and send the shop's price list.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_VENDING_ITEMS)]
public class VendingListReqHandler(
    IEntityRegistry registry,
    IVendingService vending,
    ILogger<VendingListReqHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_VENDING_ITEMS>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_VENDING_ITEMS packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        vending.VendingListReq(pc, packet.VendorAccountId);
        logger.LogInformation("VendingListReq: char {Char} browsing vendor acc {Vendor}", pc.CharacterId, packet.VendorAccountId);
        return Task.CompletedTask;
    }
}
