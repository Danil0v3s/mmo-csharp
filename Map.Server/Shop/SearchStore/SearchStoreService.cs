using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.SearchStore;

/// <summary>
/// Default <see cref="ISearchStoreService"/>. Tracks per-PC catalog
/// state; shop enumeration is deferred per PARITY-REMAINING.md §P2.2.c
/// — requires <c>IVendingService.GetAllShops</c> +
/// <c>IBuyingStoreService.GetAllShops</c> live readers. Entry points
/// are present so the search-store handler stack doesn't drift.
/// </summary>
public sealed class SearchStoreService : ISearchStoreService
{
    private readonly Dictionary<EntityId, SearchSession> _sessions = new();
    private readonly ILogger<SearchStoreService> _logger;

    public SearchStoreService(ILogger<SearchStoreService> logger) => _logger = logger;

    public bool Open(PlayerEntity searcher, byte effectId, int maxResults)
    {
        _sessions[searcher.Id] = new SearchSession { EffectId = effectId, MaxResults = maxResults };
        return true;
    }
    public int Query(PlayerEntity searcher, byte storeType, uint minPrice, uint maxPrice, int[] itemIds, int[] cardIds) => 0;
    public bool Next(PlayerEntity searcher) => false;
    public void Clear(PlayerEntity searcher) { _sessions.Remove(searcher.Id); }
    public void Close(PlayerEntity searcher) { _sessions.Remove(searcher.Id); }
    public bool Click(PlayerEntity searcher, int accountId, int storeId, int nameId) => false;
    public bool QueryNext(PlayerEntity searcher) => false;
    public bool QueryRemote(PlayerEntity searcher, int accountId) => false;
    public void ClearRemote(PlayerEntity searcher) { }

    private sealed class SearchSession
    {
        public byte EffectId;
        public int MaxResults;
    }
}
