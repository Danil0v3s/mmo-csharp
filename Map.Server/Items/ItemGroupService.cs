using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items;

/// <summary>
/// Default <see cref="IItemGroupService"/>. Caches the full
/// <c>item_group_catalog_db</c> + <c>item_group_catalog_entry_db</c>
/// at boot as <c>group → subGroup → (entries, cumulative-weight
/// array)</c>. Weighted picks are O(log N) per subgroup via
/// <see cref="Array.BinarySearch{T}(T[], T)"/>.
///
/// DBR-2b: replaces the ItemDbService no-op stub that returned 0 for
/// every group id. The legacy <see cref="IItemDbService.GetItemGroup"/>
/// surface used numeric ids; rAthena's actual catalog key is the
/// group name (string), so this service exposes name-keyed lookups.
/// </summary>
public sealed class ItemGroupService : IItemGroupService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<ItemGroupService> _logger;
    private readonly Random _rng = new();

    // groupName → subGroup → (entries, cumulative weights, totalWeight)
    private Dictionary<string, Dictionary<int, SubGroupBucket>> _bags
        = new(StringComparer.OrdinalIgnoreCase);

    public ItemGroupService(IServiceScopeFactory scopes, ILogger<ItemGroupService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty catalog.</summary>
    public ItemGroupService(ILogger<ItemGroupService> logger)
    {
        _logger = logger;
    }

    public int GroupCount => _bags.Count;

    public bool HasGroup(string groupName) =>
        !string.IsNullOrEmpty(groupName) && _bags.ContainsKey(groupName);

    public IReadOnlyList<int> SubGroupsOf(string groupName)
    {
        if (string.IsNullOrEmpty(groupName) || !_bags.TryGetValue(groupName, out var subs))
            return Array.Empty<int>();
        var list = new List<int>(subs.Keys);
        list.Sort();
        return list;
    }

    public ItemGroupCatalogEntryDbEntity? RollFromGroup(string groupName, int subGroup)
    {
        if (string.IsNullOrEmpty(groupName)) return null;
        if (!_bags.TryGetValue(groupName, out var subs)) return null;
        if (!subs.TryGetValue(subGroup, out var bucket)) return null;
        return bucket.Roll(_rng);
    }

    public IReadOnlyList<ItemGroupCatalogEntryDbEntity> RollAllSubGroups(string groupName)
    {
        if (string.IsNullOrEmpty(groupName) || !_bags.TryGetValue(groupName, out var subs))
            return Array.Empty<ItemGroupCatalogEntryDbEntity>();
        var result = new List<ItemGroupCatalogEntryDbEntity>(subs.Count);
        foreach (var (_, bucket) in subs)
        {
            var pick = bucket.Roll(_rng);
            if (pick != null) result.Add(pick);
        }
        return result;
    }

    public void Reload()
    {
        if (_scopes == null) { _bags = new(StringComparer.OrdinalIgnoreCase); return; }
        try
        {
            using var scope = _scopes.CreateScope();
            var groupsRepo = scope.ServiceProvider.GetRequiredService<IItemGroupCatalogDbRepository>();
            var groups = groupsRepo.GetAllAsync().GetAwaiter().GetResult();
            var fresh = new Dictionary<string, Dictionary<int, SubGroupBucket>>(StringComparer.OrdinalIgnoreCase);
            int totalEntries = 0;
            foreach (var g in groups)
            {
                var entries = groupsRepo.GetEntriesAsync(g.GroupName).GetAwaiter().GetResult();
                if (entries.Count == 0) continue;
                var byGroup = entries
                    .GroupBy(e => e.SubGroup)
                    .ToDictionary(grp => grp.Key, grp => SubGroupBucket.Build(grp.ToList()));
                fresh[g.GroupName] = byGroup;
                totalEntries += entries.Count;
            }
            _bags = fresh;
            _logger.LogInformation(
                "item_group_catalog_db loaded: {Groups} group(s) / {Entries} entries",
                _bags.Count, totalEntries);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_group_catalog_db load failed — catalog empty");
            _bags = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    // ---- internal weighted bucket ---------------------------------

    /// <summary>
    /// One <c>(group, subgroup)</c> bag. Entries are stored in
    /// declaration order with a parallel cumulative-weight array;
    /// a roll picks <c>rng.Next(totalWeight)</c> then binary-searches
    /// into that array — O(log N) per roll, allocation-free.
    /// </summary>
    private sealed class SubGroupBucket
    {
        private readonly ItemGroupCatalogEntryDbEntity[] _entries;
        private readonly int[] _cumWeights;
        private readonly int _totalWeight;

        private SubGroupBucket(ItemGroupCatalogEntryDbEntity[] entries, int[] cumWeights, int totalWeight)
        {
            _entries = entries;
            _cumWeights = cumWeights;
            _totalWeight = totalWeight;
        }

        public static SubGroupBucket Build(List<ItemGroupCatalogEntryDbEntity> rows)
        {
            // rAthena treats Rate<=0 as 1 (defensive — the loader normally
            // rejects 0-weight rows). Keep the same fallback here so a
            // misseeded entry doesn't tank the whole roll.
            var entries = rows.ToArray();
            var cum = new int[entries.Length];
            int running = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var w = entries[i].Rate > 0 ? entries[i].Rate : 1;
                running += w;
                cum[i] = running;
            }
            return new SubGroupBucket(entries, cum, running);
        }

        public ItemGroupCatalogEntryDbEntity? Roll(Random rng)
        {
            if (_entries.Length == 0 || _totalWeight <= 0) return null;
            // r in [1.._totalWeight] → BinarySearch for the first
            // cumulative weight >= r. (Array.BinarySearch returns
            // bitwise-complement insertion point on miss; both branches
            // collapse to "index of bucket".)
            int r = rng.Next(_totalWeight) + 1;
            int idx = Array.BinarySearch(_cumWeights, r);
            if (idx < 0) idx = ~idx;
            if (idx >= _entries.Length) idx = _entries.Length - 1;
            return _entries[idx];
        }
    }
}
