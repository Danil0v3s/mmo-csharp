using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Player presses Sell with the chosen inventory items. rAthena
/// <c>clif_parse_NpcSellListSend</c> (clif.cpp:12319). The wire packet
/// uses client_index (server_index + 2); we translate before calling
/// <see cref="IShopService.Sell"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_PC_SELL_ITEMLIST)]
public class SellItemListHandler(
    IEntityRegistry registry,
    IShopService shop,
    ILogger<SellItemListHandler> logger
) : IPacketHandler<MapSessionData, CZ_PC_SELL_ITEMLIST>
{
    public Task HandleAsync(MapSessionData session, CZ_PC_SELL_ITEMLIST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity seller)
        {
            return Task.CompletedTask;
        }

        byte result = 1; // 0 = success, 1 = failure (rAthena code)
        if (session.OpenShopNpcId is { } shopId
            && registry.Get(new EntityId(shopId)) is NpcEntity npc
            && npc.Shop != null)
        {
            var items = new List<(int InventoryIndex, int Amount)>(packet.Items.Count);
            foreach (var entry in packet.Items)
            {
                items.Add((entry.ClientIndex - 2, entry.Amount));
            }
            var op = shop.Sell(seller, items);
            result = op == ShopOpResult.Ok ? (byte)0 : (byte)1;
            if (op != ShopOpResult.Ok)
            {
                logger.LogDebug("Sell failed for char {Char}: {Reason}", seller.CharacterId, op);
            }
        }
        session.OpenShopNpcId = null;
        session.EnqueuePacket(new ZC_PC_SELL_RESULT { Result = result });
        return Task.CompletedTask;
    }
}
