using Map.Server.Entities;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Player-shop vending. Canonical entry points for rAthena
/// <c>vending.cpp</c> (768 lines, 10 functions). Vending opens a
/// player-shop stall that consumes inventory rows + a custom price
/// list; buyers see the stall on the world map and pay zeny on
/// click-purchase.
/// </summary>
public interface IVendingService
{
    /// <summary>rAthena <c>vending_openvending</c> — open the stall.</summary>
    void Update(PlayerEntity vendor, string title, IReadOnlyList<(short index, short qty, int price)> items);

    /// <summary>rAthena <c>vending_closevending</c>.</summary>
    void CloseVending(PlayerEntity vendor);

    /// <summary>rAthena <c>vending_reopen</c> — auto-trade reopen at login.</summary>
    void Reopen(PlayerEntity vendor);

    /// <summary>rAthena <c>vending_vendinglistreq</c> — client clicked a stall.</summary>
    void VendingListReq(PlayerEntity buyer, int vendorAccountId);

    /// <summary>rAthena <c>vending_purchasereq</c>.</summary>
    void PurchaseReq(PlayerEntity buyer, int vendorAccountId, IReadOnlyList<(short index, short qty)> items);

    /// <summary>rAthena <c>vending_search</c>.</summary>
    bool Search(PlayerEntity searcher, int nameId);

    /// <summary>rAthena <c>vending_searchall</c>.</summary>
    bool SearchAll(PlayerEntity searcher, int nameId);

    /// <summary>rAthena <c>do_init_vending_autotrade</c>.</summary>
    void InitAutotrade();
}
