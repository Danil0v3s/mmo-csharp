using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IItemEnchantService"/>. Loads the full
/// item_enchant tree once at boot and indexes every child table
/// so the runtime path stays allocation-free.
///
/// DBR-2e: replaces the previous "no real service" state — the
/// item-enchant UI had no source of truth, falling back to
/// hardcoded responses.
/// </summary>
public sealed class ItemEnchantService : IItemEnchantService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<ItemEnchantService> _logger;

    private Dictionary<int, ItemEnchantPipeline> _byId = new();
    private Dictionary<string, ItemEnchantPipeline> _byTarget
        = new(StringComparer.OrdinalIgnoreCase);

    public ItemEnchantService(IServiceScopeFactory scopes, ILogger<ItemEnchantService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty catalog.</summary>
    public ItemEnchantService(ILogger<ItemEnchantService> logger) { _logger = logger; }

    public bool IsLoaded => _byId.Count > 0;
    public int PipelineCount => _byId.Count;

    public ItemEnchantPipeline? GetPipelineForItem(string itemAegis)
    {
        if (string.IsNullOrEmpty(itemAegis)) return null;
        return _byTarget.TryGetValue(itemAegis, out var p) ? p : null;
    }

    public ItemEnchantPipeline? GetPipeline(int enchantId)
        => _byId.TryGetValue(enchantId, out var p) ? p : null;

    public IReadOnlyList<ItemEnchantOptionDbEntity> GetEnchantOptions(int enchantId, int slot, int enchantGrade)
    {
        if (!_byId.TryGetValue(enchantId, out var p)) return Array.Empty<ItemEnchantOptionDbEntity>();
        return p.OptionsBySlotGrade.TryGetValue((slot, enchantGrade), out var list)
            ? list
            : Array.Empty<ItemEnchantOptionDbEntity>();
    }

    public (int chance, int price, IReadOnlyList<ItemEnchantMaterialDbEntity> materials)? GetResetCost(int enchantId)
    {
        if (!_byId.TryGetValue(enchantId, out var p)) return null;
        var mats = p.MaterialsBySlot.TryGetValue(-1, out var list)
            ? (IReadOnlyList<ItemEnchantMaterialDbEntity>)list
            : Array.Empty<ItemEnchantMaterialDbEntity>();
        return (p.ResetChance, p.ResetPrice, mats);
    }

    public (int price, IReadOnlyList<ItemEnchantMaterialDbEntity> materials, int? orderIndex)? GetSlotCost(int enchantId, int slot)
    {
        if (!_byId.TryGetValue(enchantId, out var p)) return null;
        if (!p.SlotByIndex.TryGetValue(slot, out var slotRow)) return null;
        var mats = p.MaterialsBySlot.TryGetValue(slot, out var list)
            ? (IReadOnlyList<ItemEnchantMaterialDbEntity>)list
            : Array.Empty<ItemEnchantMaterialDbEntity>();
        return (slotRow.Price, mats, slotRow.OrderIndex);
    }

    public void Reload()
    {
        _byId = new();
        _byTarget = new(StringComparer.OrdinalIgnoreCase);
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IItemEnchantDbRepository>();
            var roots = repo.GetAllAsync().GetAwaiter().GetResult();
            // Bulk-fetch all child rows once, then bucket per pipeline.
            // For 140 pipelines this is much cheaper than 140×4 round trips.
            var allTargets = repo.GetAllTargetsAsync().GetAwaiter().GetResult();
            var targetsByPipeline = allTargets
                .GroupBy(t => t.EnchantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int totalOptions = 0;
            foreach (var root in roots)
            {
                var materials = repo.GetMaterialsAsync(root.EnchantId).GetAwaiter().GetResult();
                var slots = repo.GetSlotsAsync(root.EnchantId).GetAwaiter().GetResult();
                var options = repo.GetOptionsAsync(root.EnchantId).GetAwaiter().GetResult();
                totalOptions += options.Count;

                var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (targetsByPipeline.TryGetValue(root.EnchantId, out var tlist))
                    foreach (var t in tlist) targets.Add(t.ItemAegis);

                var pipeline = new ItemEnchantPipeline
                {
                    EnchantId = root.EnchantId,
                    MinimumRefine = root.MinimumRefine,
                    ResetChance = root.ResetChance,
                    ResetPrice = root.ResetPrice,
                    Targets = targets,
                    MaterialsBySlot = materials.GroupBy(m => m.Slot).ToDictionary(g => g.Key, g => g.ToList()),
                    SlotByIndex = slots.ToDictionary(s => s.Slot, s => s),
                    OptionsBySlotGrade = options
                        .GroupBy(o => (o.Slot, o.EnchantGrade))
                        .ToDictionary(g => g.Key, g => g.ToList()),
                };

                _byId[root.EnchantId] = pipeline;
                foreach (var aegis in targets) _byTarget[aegis] = pipeline;
            }
            _logger.LogInformation(
                "item_enchant catalog loaded: {Pipelines} pipeline(s) / {Targets} target(s) / {Options} option row(s)",
                _byId.Count, _byTarget.Count, totalOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "item_enchant catalog load failed — enchant queries will return null");
        }
    }
}
