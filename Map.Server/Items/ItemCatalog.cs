using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DbItem = Core.Database.Entities.ItemEntity;

namespace Map.Server.Items;

/// <summary>
/// Default <see cref="IItemCatalog"/>. Same lifecycle pattern as
/// <see cref="Mob.MobDb"/>: singleton holding an immutable snapshot,
/// scoped repository accessed via <see cref="IServiceScopeFactory"/>,
/// sync block on the startup thread for the one-time load.
///
/// At ~28K rows for the renewal item_db, the snapshot is small enough
/// (millions of bytes, not gigabytes) that holding the whole table in
/// memory is the right call — gameplay lookups are O(1) and there's no
/// reason to hit the DB on every drop or pickup.
/// </summary>
public sealed class ItemCatalog : IItemCatalog
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ItemCatalog> _logger;
    private volatile Snapshot _snapshot;

    public ItemCatalog(IServiceScopeFactory scopeFactory, ILogger<ItemCatalog> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _snapshot = LoadSnapshot();
    }

    public int Count => _snapshot.ById.Count;

    public DbItem? Get(uint itemId) =>
        _snapshot.ById.TryGetValue(itemId, out var e) ? e : null;

    public DbItem? GetByAegisName(string aegisName) =>
        aegisName is null ? null :
        _snapshot.ByName.TryGetValue(aegisName, out var e) ? e : null;

    public IEnumerable<DbItem> All() => _snapshot.ById.Values;

    public void Reload() => _snapshot = LoadSnapshot();

    private Snapshot LoadSnapshot()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var rows = repository.GetAllAsync().GetAwaiter().GetResult();

        var byId = new Dictionary<uint, DbItem>(rows.Count);
        foreach (var row in rows)
        {
            byId[row.Id] = row;
        }
        var byName = new Dictionary<string, DbItem>(byId.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var row in byId.Values)
        {
            byName[row.NameAegis] = row;
        }
        _logger.LogInformation("ItemCatalog loaded {Count} entries", byId.Count);
        return new Snapshot(byId, byName);
    }

    private sealed record Snapshot(
        IReadOnlyDictionary<uint, DbItem> ById,
        IReadOnlyDictionary<string, DbItem> ByName);
}
