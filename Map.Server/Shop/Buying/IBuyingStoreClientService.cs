using Map.Server.Entities;

namespace Map.Server.Shop.Buying;

/// <summary>
/// Buying-store → client emit hub. Mirrors rAthena's <c>clif_buyingstore_*</c> emitters. The store
/// sign is an area broadcast; the owner's item list + the open-failure are sent to the buyer.
/// </summary>
public interface IBuyingStoreClientService
{
    /// <summary>rAthena <c>clif_buyingstore_myitemlist</c> (ZC_MYITEMLIST_BUYING_STORE) + entry sign —
    /// confirm the store to its owner with their offers + escrow limit, and show the store sign in
    /// view.</summary>
    void OpenStore(PlayerEntity buyer, int zenyLimit, string title,
        IReadOnlyList<Core.Server.Packets.Out.ZC.BuyingStoreEntry> items);

    /// <summary>rAthena <c>clif_buyingstore_disappear_entry</c> (ZC_DISAPPEAR_BUYING_STORE_ENTRY) —
    /// remove the buyer's store sign from view.</summary>
    void CloseStore(PlayerEntity buyer);

    /// <summary>rAthena <c>clif_buyingstore_open_failed</c> (ZC_FAILED_OPEN_BUYING_STORE) — the store
    /// couldn't be created.</summary>
    void OpenFailed(PlayerEntity buyer, Core.Server.Packets.Out.ZC.BuyingStoreOpenResult result);

    /// <summary>rAthena <c>clif_buyingstore_itemlist</c> (ZC_ACK_ITEMLIST_BUYING_STORE) — send the
    /// store's offers (+ id + escrow limit) to a visitor who clicked it.</summary>
    void SendVisitorList(PlayerEntity visitor, int ownerAccountId, uint storeId, int zenyLimit,
        IReadOnlyList<Core.Server.Packets.Out.ZC.BuyingStoreEntry> items);

    /// <summary>rAthena <c>clif_buyingstore_delete_item</c> (ZC_ITEM_DELETE_BUYING_STORE) — tell the
    /// seller an item was sold out of their bag.</summary>
    void SendSellerDelete(PlayerEntity seller, short clientIndex, short amount, int price);

    /// <summary>rAthena <c>clif_buyingstore_trade_failed_seller</c> (ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER)
    /// — a sell-in attempt failed.</summary>
    void SendSellerFail(PlayerEntity seller, Core.Server.Packets.Out.ZC.BuyStoreSellResult result, short nameId);
}
