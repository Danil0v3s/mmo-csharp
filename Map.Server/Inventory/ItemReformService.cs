using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IItemReformService"/>. Loads the full
/// item_reform_db + item_reform_base_db catalog at boot and exposes
/// the per-opener lookup primitive that the Reform UI handler
/// consumes.
/// </summary>
public sealed class ItemReformService : IItemReformService
{
    // ResultItemAegis (opener) → config. Case-insensitive.
    private readonly Dictionary<string, ItemReformConfig> _byOpener =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ItemReformService> _logger;

    public int CatalogCount => _byOpener.Count;

    public ItemReformService(IServiceScopeFactory scopes, ILogger<ItemReformService> logger)
    {
        _logger = logger;
        Load(scopes);
    }

    private void Load(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IItemReformDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            var allBases = repo.GetAllBasesAsync().GetAwaiter().GetResult();

            // Group bases by parent result aegis.
            var basesByResult = new Dictionary<string, List<ItemReformBase>>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in allBases)
            {
                if (!basesByResult.TryGetValue(b.ResultItemAegis, out var list))
                {
                    list = new List<ItemReformBase>();
                    basesByResult[b.ResultItemAegis] = list;
                }
                list.Add(new ItemReformBase(
                    b.BaseItemAegis, b.MaximumRefine, b.ChangeRefine,
                    b.ResultItemOverride, b.RandomOptionGroup,
                    b.ClearSlots, b.RemoveEnchantgrade, b.CardsAllowed));
            }

            foreach (var p in parents)
            {
                var bases = basesByResult.GetValueOrDefault(p.ResultItemAegis) ?? new List<ItemReformBase>();
                _byOpener[p.ResultItemAegis] = new ItemReformConfig(p.ResultItemAegis, bases);
            }

            _logger.LogInformation(
                "item_reform_db loaded: {Parents} reform pipelines / {Bases} base variants",
                _byOpener.Count, allBases.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_reform_db load failed; reform pipeline disabled");
        }
    }

    public ItemReformConfig? GetReformConfig(string openerItemAegis)
        => _byOpener.GetValueOrDefault(openerItemAegis);
}
