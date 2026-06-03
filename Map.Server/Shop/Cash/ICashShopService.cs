using Map.Server.Entities;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Result of a cash-shop purchase, mirroring rAthena
/// <c>e_CASHSHOP_ACK</c> (clif.hpp:78). The map server resolves the
/// per-condition code; the ZC ack/result packet emit consumes it
/// (PACKET seam → PACKET-08).
/// </summary>
public enum CashShopResult : byte
{
    /// <summary>rAthena <c>ERROR_TYPE_NONE</c> — the purchase completed.</summary>
    Success = 0,
    /// <summary>rAthena <c>ERROR_TYPE_NPC</c> — the shop NPC does not exist.</summary>
    NoShop = 1,
    /// <summary>rAthena <c>ERROR_TYPE_SYSTEM</c> — malformed request (bad tab / catalog not loaded).</summary>
    System = 2,
    /// <summary>rAthena <c>ERROR_TYPE_INVENTORY_WEIGHT</c> — overweight or no inventory space.</summary>
    InventoryWeight = 3,
    /// <summary>rAthena <c>ERROR_TYPE_EXCHANGE</c> — busy in a trade.</summary>
    Exchange = 4,
    /// <summary>rAthena <c>ERROR_TYPE_ITEM_ID</c> — the item information was incorrect.</summary>
    ItemId = 5,
    /// <summary>rAthena <c>ERROR_TYPE_MONEY</c> — not enough cash / kafra points.</summary>
    Money = 6,
    /// <summary>rAthena <c>ERROR_TYPE_AMOUNT</c> — bad amount / over the stack limit.</summary>
    Amount = 7,
    /// <summary>rAthena <c>ERROR_TYPE_PURCHASE_FAIL</c> — item not in the catalog / wrong tab.</summary>
    PurchaseFail = 8,
}

/// <summary>
/// Cash Shop (real-money cosmetic / VIP shop). Canonical entry
/// points for rAthena <c>cashshop.cpp</c> (672 lines, 7 public).
///
/// rAthena loads <c>cashshop_db.yml</c> + a sale schedule. The map
/// server handles the "buy these IDs, pay this many points" flow
/// + notify-login-sale broadcasts. Persistence (purchase log) goes
/// through char-server.
/// </summary>
public interface ICashShopService
{
    /// <summary>rAthena <c>cashshop_buylist</c> — resolve each item's catalog price (applying the
    /// active sale discount for the Sale tab), validate inventory space + weight for the whole
    /// basket, debit kafra-then-cash points (kafra capped to <paramref name="kafraPay"/>), grant the
    /// items, and decrement any consumed sale stock. Returns the per-condition
    /// <see cref="CashShopResult"/> (<see cref="CashShopResult.Success"/> on a completed purchase) —
    /// no mutation happens on any rejection (all-or-nothing).</summary>
    CashShopResult BuyList(PlayerEntity pc, int kafraPay, IReadOnlyList<(int itemId, int qty, byte tab)> items);

    /// <summary>rAthena <c>CashShopDatabase::parseBodyNode</c> — (re)load the cashshop catalog.</summary>
    void Reload();

    /// <summary>rAthena <c>cashshop_reloaddb</c>.</summary>
    void ReloadDb();

    /// <summary>rAthena <c>sale_remove_item</c>.</summary>
    bool SaleRemoveItem(int itemId);

    /// <summary>rAthena <c>sale_notify_login</c> — emit the active-sale list to a logging-in player.</summary>
    void SaleNotifyLogin(PlayerEntity pc);

    /// <summary>rAthena <c>sale_add_item</c> — schedule a timed sale window.</summary>
    void SaleAddItem(int itemId, int amount, DateTime startAt, DateTime endAt);

    /// <summary>rAthena <c>sale_find_item</c> — true when an active sale exists for the item.</summary>
    bool SaleFindItem(int itemId);
}
