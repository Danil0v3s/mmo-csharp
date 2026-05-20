using Map.Server.Entities;

namespace Map.Server.Shop.Cash;

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
    /// <summary>rAthena <c>cashshop_buylist</c>.</summary>
    bool BuyList(PlayerEntity pc, IReadOnlyList<(int itemId, int qty, byte tab)> items);

    /// <summary>rAthena <c>CashShopDatabase::parseBodyNode</c> — load cashshop_db row.</summary>
    void Reload();

    /// <summary>rAthena <c>cashshop_reloaddb</c>.</summary>
    void ReloadDb();

    /// <summary>rAthena <c>sale_remove_item</c>.</summary>
    bool SaleRemoveItem(int itemId);

    /// <summary>rAthena <c>sale_notify_login</c>.</summary>
    void SaleNotifyLogin(PlayerEntity pc);
}
