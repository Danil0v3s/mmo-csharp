using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Cash;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Buy item(s) from the cash shop. rAthena <c>clif_parse_cashshop_buy</c> (clif.cpp, 0x0848) →
/// <c>cashshop_buylist</c>. Forwards the basket (item / amount / tab) + the requested kafra-point
/// split to <see cref="ICashShopService.BuyList"/>, then emits the result + the resulting balances
/// (<c>clif_cashshop_result</c>).
/// </summary>
[PacketHandler(PacketHeader.CZ_PC_BUY_CASHITEM_LIST)]
public class BuyCashItemHandler(
    IEntityRegistry registry,
    ICashShopService cashShop,
    ICashShopClientService client,
    ILogger<BuyCashItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_PC_BUY_CASHITEM_LIST>
{
    public Task HandleAsync(MapSessionData session, CZ_PC_BUY_CASHITEM_LIST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        if (packet.Lines.Count == 0) return Task.CompletedTask;

        // rAthena clif_parse_cashshop_buy gates a busy (trading) player before pricing.
        if (session.Trade != null)
        {
            client.SendBuyResult(pc, 0, CashShopBuyResult.PcState);
            return Task.CompletedTask;
        }

        var items = packet.Lines
            .Select(l => (l.ItemId, l.Amount, (byte)l.Tab))
            .ToList();
        var firstId = (uint)packet.Lines[0].ItemId;

        var result = cashShop.BuyList(pc, packet.KafraPoints, items);
        client.SendBuyResult(pc, result == CashShopResult.Success ? firstId : 0, Map(result));

        logger.LogInformation("BuyCashItem: char {Char} bought {N} line(s), kafraPay={Kafra} → {Result}",
            pc.CharacterId, items.Count, packet.KafraPoints, result);
        return Task.CompletedTask;
    }

    /// <summary>Map the service's <see cref="CashShopResult"/> (the <c>e_CASHSHOP_ACK</c> / NPC-shop
    /// table) to the cash-shop-button buy result enum <see cref="CashShopBuyResult"/>
    /// (<c>CASHSHOP_RESULT_*</c>). The service collapses weight/slot rejections into
    /// <see cref="CashShopResult.InventoryWeight"/> (split → GP-CASHSHOP-SLOT-WEIGHT-CODE).</summary>
    private static CashShopBuyResult Map(CashShopResult r) => r switch
    {
        CashShopResult.Success => CashShopBuyResult.Success,
        CashShopResult.Money => CashShopBuyResult.ShortageCash,
        CashShopResult.ItemId => CashShopBuyResult.UnknownItem,
        CashShopResult.PurchaseFail => CashShopBuyResult.UnknownItem,
        CashShopResult.InventoryWeight => CashShopBuyResult.InventoryWeight,
        CashShopResult.Exchange => CashShopBuyResult.PcState,
        CashShopResult.Amount => CashShopBuyResult.OverProductTotalCnt,
        CashShopResult.NoShop => CashShopBuyResult.System,
        _ => CashShopBuyResult.System,
    };
}
