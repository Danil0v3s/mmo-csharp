using Map.Server.Entities;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Vending → client emit hub. Mirrors rAthena's <c>clif_vending_*</c> emitters — one method per wire
/// packet. The stall sign is an area broadcast; the open ack is sent to the vendor.
/// </summary>
public interface IVendingClientService
{
    /// <summary>rAthena <c>clif_showvendingboard</c> (ZC_STORE_ENTRY) — show the stall sign over the
    /// vendor to everyone in view, plus <c>clif_openvending_ack</c> success to the vendor.</summary>
    void OpenStall(PlayerEntity vendor, string title);

    /// <summary>rAthena <c>clif_closevendingboard</c> (ZC_DISAPPEAR_ENTRY) — remove the vendor's stall
    /// sign from everyone in view.</summary>
    void CloseStall(PlayerEntity vendor);

    /// <summary>rAthena <c>clif_openvending_ack</c> (ZC_ACK_OPENSTORE2) — open result to the vendor
    /// (0 = success).</summary>
    void OpenAck(PlayerEntity vendor, byte result);

    /// <summary>rAthena <c>clif_vendinglist</c> (ZC_PC_PURCHASE_ITEMLIST_FROMMC) — send the shop's price
    /// list to a buyer who clicked the stall.</summary>
    void SendVendingList(PlayerEntity buyer, int ownerAccountId, IReadOnlyList<Core.Server.Packets.Out.ZC.VendingListEntry> items);

    /// <summary>rAthena <c>clif_buyvending</c> (ZC_PC_PURCHASE_RESULT_FROMMC) — purchase result to the
    /// buyer (success / not-enough-zeny / overweight / out-of-stock / store-incorrect).</summary>
    void SendPurchaseResult(PlayerEntity buyer, short clientIndex, short amount, Core.Server.Packets.Out.ZC.VendPurchaseResult result);

    /// <summary>rAthena <c>clif_vendingreport</c> (ZC_DELETEITEM_FROM_MCSTORE) — tell the vendor an item
    /// was bought from their shop.</summary>
    void SendVendorReport(PlayerEntity vendor, short clientIndex, short amount);
}
