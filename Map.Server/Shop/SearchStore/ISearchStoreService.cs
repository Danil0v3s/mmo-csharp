using Map.Server.Entities;

namespace Map.Server.Shop.SearchStore;

/// <summary>
/// Universal Catalog (search-store) — query vending + buying-store
/// shops by item id / cost / refine. Canonical entry points for
/// rAthena <c>searchstore.cpp</c> (361 lines, 9 functions).
///
/// rAthena's `searchstore_query` walks every active vendor on every
/// map and returns matches. The C# port wires through
/// <see cref="Vending.IVendingService"/> + the (data-pending) buying
/// store registry once they expose enumerable shop lists.
/// </summary>
public interface ISearchStoreService
{
    /// <summary>rAthena <c>searchstore_open</c> — client opened the catalog.</summary>
    bool Open(PlayerEntity searcher, byte effectId, int maxResults);

    /// <summary>rAthena <c>searchstore_query</c> — execute a search.</summary>
    int Query(PlayerEntity searcher, byte storeType, uint minPrice, uint maxPrice, int[] itemIds, int[] cardIds);

    /// <summary>rAthena <c>searchstore_next</c> — pagination.</summary>
    bool Next(PlayerEntity searcher);

    /// <summary>rAthena <c>searchstore_clear</c>.</summary>
    void Clear(PlayerEntity searcher);

    /// <summary>rAthena <c>searchstore_close</c>.</summary>
    void Close(PlayerEntity searcher);

    /// <summary>rAthena <c>searchstore_click</c> — picked a result row.</summary>
    bool Click(PlayerEntity searcher, int accountId, int storeId, int nameId);

    /// <summary>rAthena <c>searchstore_querynext</c>.</summary>
    bool QueryNext(PlayerEntity searcher);

    /// <summary>rAthena <c>searchstore_queryremote</c> — cross-map search.</summary>
    bool QueryRemote(PlayerEntity searcher, int accountId);

    /// <summary>rAthena <c>searchstore_clearremote</c>.</summary>
    void ClearRemote(PlayerEntity searcher);
}
