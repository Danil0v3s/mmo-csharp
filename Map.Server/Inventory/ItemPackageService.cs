using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IItemPackageService"/>. Loads the full
/// item_package_db + item_package_entry_db catalog at boot and
/// exposes per-opener lookups. Items inside each package are
/// pre-grouped at boot so the consumer doesn't reshape on every use.
/// </summary>
public sealed class ItemPackageService : IItemPackageService
{
    private readonly Dictionary<string, ItemPackageContents> _byOpener =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ItemPackageService> _logger;

    public int CatalogCount => _byOpener.Count;

    public ItemPackageService(IServiceScopeFactory scopes, ILogger<ItemPackageService> logger)
    {
        _logger = logger;
        Load(scopes);
    }

    private void Load(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IItemPackageDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            var allEntries = repo.GetAllEntriesAsync().GetAwaiter().GetResult();

            // Pre-group entries: ItemAegis (opener) → GroupId → entries.
            var byOpenerGroup =
                new Dictionary<string, Dictionary<int, List<ItemPackageEntry>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in allEntries)
            {
                if (!byOpenerGroup.TryGetValue(e.ItemAegis, out var groups))
                {
                    groups = new Dictionary<int, List<ItemPackageEntry>>();
                    byOpenerGroup[e.ItemAegis] = groups;
                }
                if (!groups.TryGetValue(e.GroupId, out var list))
                {
                    list = new List<ItemPackageEntry>();
                    groups[e.GroupId] = list;
                }
                list.Add(new ItemPackageEntry(
                    e.ContainedItemAegis, e.Amount, e.Refine, e.RentalHours, e.RandomOptionGroup));
            }

            foreach (var p in parents)
            {
                if (!byOpenerGroup.TryGetValue(p.ItemAegis, out var groups) || groups.Count == 0)
                {
                    _byOpener[p.ItemAegis] = new ItemPackageContents(p.ItemAegis, Array.Empty<ItemPackageGroup>());
                    continue;
                }
                var groupList = groups
                    .OrderBy(kv => kv.Key)
                    .Select(kv => new ItemPackageGroup(kv.Key, kv.Value))
                    .ToList();
                _byOpener[p.ItemAegis] = new ItemPackageContents(p.ItemAegis, groupList);
            }

            _logger.LogInformation(
                "item_package_db loaded: {Packages} packages / {Entries} entries",
                _byOpener.Count, allEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_package_db load failed; package pipeline disabled");
        }
    }

    public ItemPackageContents? GetContents(string openerItemAegis)
        => _byOpener.GetValueOrDefault(openerItemAegis);
}
