using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Map.Server.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items;

/// <summary>
/// Default <see cref="IMapDropService"/>. Loads the DB-8i typed
/// catalogs (map_drop_db + map_drop_entry_db) at boot and rolls them
/// on every mob death.
///
/// DBR-1g: the prior code path didn't consult map_drops.yml at all —
/// per-map global drops + per-mob filtered drops were entirely
/// data-pending. Wiring this closes the gap.
/// </summary>
public sealed class MapDropService : IMapDropService
{
    private readonly IItemDropService _itemDrops;
    private readonly IItemCatalog _catalog;
    private readonly ILogger<MapDropService> _logger;
    private readonly Random _rng = new();

    /// <summary>map_name → list of entries (ordered by EntryIndex).</summary>
    private readonly Dictionary<string, MapDropList> _byMap = new(System.StringComparer.OrdinalIgnoreCase);

    public bool HasData => _byMap.Count > 0;

    public MapDropService(
        IItemDropService itemDrops,
        IItemCatalog catalog,
        IServiceScopeFactory scopes,
        ILogger<MapDropService> logger)
    {
        _itemDrops = itemDrops;
        _catalog = catalog;
        _logger = logger;
        LoadFromDb(scopes);
    }

    /// <summary>Test ctor — leaves the cache empty (RollAndDrop becomes a no-op).</summary>
    public MapDropService(IItemDropService itemDrops, IItemCatalog catalog, ILogger<MapDropService> logger)
    {
        _itemDrops = itemDrops;
        _catalog = catalog;
        _logger = logger;
    }

    private void LoadFromDb(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMapDropDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            if (parents.Count == 0)
            {
                _logger.LogInformation("map_drop_db is empty — map-level drop overrides disabled");
                return;
            }
            // Single GetAllEntriesAsync round-trip instead of 17 GetEntriesAsync calls.
            var allEntries = repo.GetAllEntriesAsync().GetAwaiter().GetResult();

            foreach (var p in parents)
            {
                _byMap[p.MapName] = new MapDropList();
            }
            foreach (var e in allEntries)
            {
                if (!_byMap.TryGetValue(e.MapName, out var list)) continue;
                if (e.MobFilterAegis == null)
                {
                    list.Global.Add(new DropEntry(e.ItemAegis, e.Rate));
                }
                else
                {
                    if (!list.PerMob.TryGetValue(e.MobFilterAegis, out var perMobList))
                    {
                        perMobList = new List<DropEntry>();
                        list.PerMob[e.MobFilterAegis] = perMobList;
                    }
                    perMobList.Add(new DropEntry(e.ItemAegis, e.Rate));
                }
            }
            _logger.LogInformation(
                "Loaded {Maps} maps from map_drop_db ({Entries} entries)",
                _byMap.Count, allEntries.Count);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "map_drop_db load failed — map drop overrides disabled");
        }
    }

    public int RollAndDrop(MapData map, MobEntity mob, PlayerEntity? lastHitter, short x, short y)
    {
        if (!HasData || map == null || mob == null) return 0;
        if (!_byMap.TryGetValue(map.Name, out var list)) return 0;

        int dropped = 0;
        // GlobalDrops: every mob on the map participates.
        foreach (var entry in list.Global)
        {
            if (TryRoll(entry, mob, lastHitter, x, y)) dropped++;
        }
        // Per-mob filtered drops: match the dead mob's aegis name.
        if (mob.DbEntry != null && list.PerMob.TryGetValue(mob.DbEntry.AegisName, out var perMob))
        {
            foreach (var entry in perMob)
            {
                if (TryRoll(entry, mob, lastHitter, x, y)) dropped++;
            }
        }
        return dropped;
    }

    private bool TryRoll(DropEntry entry, MobEntity mob, PlayerEntity? lastHitter, short x, short y)
    {
        if (entry.Rate <= 0) return false;
        // rAthena: map_drops rate is per 100000. NOTE: map-drop rolls
        // bypass the global drop_rate multipliers (battle_config
        // item_rate_*), unlike the mob's own drop table — see
        // rAthena mob.cpp:mob_dead near the map_drops loop.
        if (_rng.Next(100_000) >= entry.Rate) return false;
        var itemRow = _catalog.GetByAegisName(entry.ItemAegis);
        if (itemRow == null)
        {
            _logger.LogWarning(
                "map_drop {Map}/{Mob} drops unknown item '{Item}' (no row in item_db)",
                mob.MapId, mob.ClassId, entry.ItemAegis);
            return false;
        }
        _itemDrops.DropOnFloor(
            mob.MapId, x, y,
            itemId: (int)itemRow.Id,
            amount: 1,
            subX: (byte)_rng.Next(0, 16),
            subY: (byte)_rng.Next(0, 16),
            ownerCharId: lastHitter?.CharacterId ?? 0,
            ownerPartyId: lastHitter?.PartyId ?? 0);
        return true;
    }

    private sealed class MapDropList
    {
        public List<DropEntry> Global { get; } = new();
        public Dictionary<string, List<DropEntry>> PerMob { get; } = new(System.StringComparer.OrdinalIgnoreCase);
    }

    private readonly record struct DropEntry(string ItemAegis, int Rate);
}
