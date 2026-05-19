using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Player presses Buy after picking items in the shop dialog. rAthena
/// <c>clif_parse_NpcBuyListSend</c> (clif.cpp:12271). We pull the
/// active shop from <see cref="MapSessionData.OpenShopNpcId"/>, run
/// the buy through <see cref="IShopService"/>, and reply with the
/// rAthena result code.
/// </summary>
[PacketHandler(PacketHeader.CZ_PC_PURCHASE_ITEMLIST)]
public class PurchaseItemListHandler(
    IEntityRegistry registry,
    IShopService shop,
    ILogger<PurchaseItemListHandler> logger
) : IPacketHandler<MapSessionData, CZ_PC_PURCHASE_ITEMLIST>
{
    public Task HandleAsync(MapSessionData session, CZ_PC_PURCHASE_ITEMLIST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity buyer)
        {
            return Task.CompletedTask;
        }

        byte result = 1; // default to NotEnoughZeny on fail
        if (session.OpenShopNpcId is { } shopId
            && registry.Get(new EntityId(shopId)) is NpcEntity npc
            && npc.Shop != null)
        {
            var items = new List<(int, int)>(packet.Items.Count);
            foreach (var entry in packet.Items)
            {
                items.Add((entry.NameId, entry.Amount));
            }
            var op = shop.Buy(buyer, npc.Shop, items);
            result = op switch
            {
                ShopOpResult.Ok => 0,
                ShopOpResult.NotEnoughZeny => 1,
                ShopOpResult.InventoryFull => 2,
                _ => 3,
            };
            if (op != ShopOpResult.Ok)
            {
                logger.LogDebug("Buy failed for char {Char}: {Reason}", buyer.CharacterId, op);
            }
        }
        session.OpenShopNpcId = null;
        session.EnqueuePacket(new ZC_PC_PURCHASE_RESULT { Result = result });
        return Task.CompletedTask;
    }
}
