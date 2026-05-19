using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Player chose Buy or Sell on an NPC's buy/sell dialog. rAthena
/// <c>clif_parse_NpcBuySellSelected</c> (clif.cpp:12230).
///
/// Type 0 = buy → send <c>ZC_PC_PURCHASE_ITEMLIST</c> with the shop
/// catalog. Type 1 = sell → send <c>ZC_PC_SELL_ITEMLIST</c> with the
/// player's inventory at the half-price sell rate.
/// </summary>
[PacketHandler(PacketHeader.CZ_ACK_SELECT_DEALTYPE)]
public class SelectDealTypeHandler(
    IEntityRegistry registry,
    IItemCatalog catalog,
    ILogger<SelectDealTypeHandler> logger
) : IPacketHandler<MapSessionData, CZ_ACK_SELECT_DEALTYPE>
{
    /// <summary>rAthena <c>battle_config.sell_ratio</c> — kept in sync with ShopService.</summary>
    private const int SellRatioPercent = 50;

    public Task HandleAsync(MapSessionData session, CZ_ACK_SELECT_DEALTYPE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        // Resolve the NPC + its shop registration.
        var npc = registry.Get(new EntityId(packet.NpcId)) as NpcEntity;
        if (npc?.Shop == null)
        {
            logger.LogDebug("Shop dialog on non-shop NPC {NpcId}", packet.NpcId);
            return Task.CompletedTask;
        }

        session.OpenShopNpcId = packet.NpcId;

        if (packet.DealType == 0)
        {
            // Buy: send catalog.
            var rows = new List<ZC_PC_PURCHASE_ITEMLIST.ShopRow>(npc.Shop.Items.Count);
            foreach (var item in npc.Shop.Items)
            {
                var row = catalog.Get((uint)item.ItemId);
                rows.Add(new ZC_PC_PURCHASE_ITEMLIST.ShopRow(
                    Price: item.Price,
                    DiscountPrice: item.Discount ?? item.Price,
                    ItemType: ItemTypeCode(row?.Type),
                    NameId: (ushort)item.ItemId));
            }
            session.EnqueuePacket(new ZC_PC_PURCHASE_ITEMLIST { Rows = rows });
        }
        else
        {
            // Sell: send inventory at the rAthena sell ratio.
            var rows = new List<ZC_PC_SELL_ITEMLIST.SellRow>();
            if (session.Inventory is { } inv)
            {
                for (var i = 0; i < inv.Count; i++)
                {
                    var item = inv[i];
                    if (item.Amount <= 0) continue;
                    var row = catalog.Get(item.NameId);
                    var buyPrice = (int)(row?.PriceBuy ?? 0);
                    var sellPrice = buyPrice * SellRatioPercent / 100;
                    rows.Add(new ZC_PC_SELL_ITEMLIST.SellRow(
                        ClientIndex: (ushort)(i + 2),
                        Price: sellPrice,
                        OverchargePrice: sellPrice));
                }
            }
            session.EnqueuePacket(new ZC_PC_SELL_ITEMLIST { Rows = rows });
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps rAthena <c>item_db.Type</c> strings to the wire numeric
    /// type the client expects (rough subset). Anything unknown maps
    /// to 4 (etc / common).
    /// </summary>
    private static byte ItemTypeCode(string? type) => type switch
    {
        "Healing" => 0,
        "Usable" => 2,
        "Etc" => 3,
        "Weapon" => 4,
        "Armor" => 5,
        "Card" => 6,
        "PetEgg" => 7,
        "PetArmor" => 8,
        "Ammo" => 10,
        _ => 3,
    };
}
