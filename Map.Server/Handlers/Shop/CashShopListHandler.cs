using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Cash;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Send the cash-shop catalog (requested right after the shop opens). rAthena
/// <c>clif_parse_cashshop_list_request</c> (clif.cpp, 0x08c9) → <c>clif_cashshop_list</c> +
/// <c>sale_notify_login</c>. Emits one <c>ZC_ACK_SCHEDULER_CASHITEM</c> per non-empty tab, then the
/// active-sale banner.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CASHSHOP_ITEMLIST)]
public class CashShopListHandler(
    IEntityRegistry registry,
    ICashShopService cashShop,
    ICashShopClientService client,
    ILogger<CashShopListHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CASHSHOP_ITEMLIST>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CASHSHOP_ITEMLIST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        var tabs = cashShop.CatalogTabs();
        client.SendCatalog(pc, tabs);
        cashShop.SaleNotifyLogin(pc);
        logger.LogDebug("CashShopList: char {Char} got {N} tab(s)", pc.CharacterId, tabs.Count);
        return Task.CompletedTask;
    }
}
