using Map.Server.Entities;

namespace Map.Server.Shop.Buying;

/// <summary>
/// Player buying-store stalls (the "I will pay X for Y" reverse
/// vending). Canonical entry points for rAthena
/// <c>buyingstore.cpp</c> (832 lines, 10 functions).
/// </summary>
public interface IBuyingStoreService
{
    /// <summary>rAthena <c>buyingstore_open</c>.</summary>
    void Open(PlayerEntity buyer, byte effectId);

    /// <summary>rAthena <c>buyingstore_update</c> — confirm offers list.</summary>
    void Update(PlayerEntity buyer, string title, long zenyLimit,
        IReadOnlyList<(int nameId, short amount, int price)> offers);

    /// <summary>rAthena <c>buyingstore_close</c>.</summary>
    void Close(PlayerEntity buyer);

    /// <summary>rAthena <c>buyingstore_reopen</c>.</summary>
    void Reopen(PlayerEntity buyer);

    /// <summary>rAthena <c>buyingstore_trade</c> — seller accepted offer.</summary>
    void Trade(PlayerEntity seller, int buyerAccountId, uint storeId,
        IReadOnlyList<(short index, short amount)> items);

    /// <summary>rAthena <c>buyingstore_search</c>.</summary>
    bool Search(PlayerEntity searcher, int nameId);

    /// <summary>rAthena <c>buyingstore_searchall</c>.</summary>
    bool SearchAll(PlayerEntity searcher, int nameId);

    /// <summary>rAthena <c>do_init_buyingstore_autotrade</c>.</summary>
    void InitAutotrade();
}
