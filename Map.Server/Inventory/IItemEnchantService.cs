using Core.Database.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// rAthena item-enchant pipeline (see <c>item_enchant.cpp</c>
/// + <c>db/re/item_enchant.yml</c>). Each pipeline is a 5-table
/// shape: root row (Id, MinimumRefine, Reset.{Chance,Price}),
/// list of target items, materials (per-slot + reset), slot
/// costs/order, and per-slot enchantable options.
///
/// DBR-2e: sources from <see cref="Core.Database.Repositories.Api.IItemEnchantDbRepository"/>
/// (140 pipelines, 4657 option rows seeded by DB-8g). Cache built
/// once at boot.
/// </summary>
public interface IItemEnchantService
{
    /// <summary>True if at least one pipeline was loaded.</summary>
    bool IsLoaded { get; }

    /// <summary>Number of pipelines in the cache.</summary>
    int PipelineCount { get; }

    /// <summary>
    /// Resolve the pipeline whose <c>TargetItems</c> contain
    /// <paramref name="itemAegis"/>. Returns null if no pipeline
    /// targets the item.
    /// </summary>
    ItemEnchantPipeline? GetPipelineForItem(string itemAegis);

    /// <summary>Pipeline by id (rAthena <c>EnchantId</c>).</summary>
    ItemEnchantPipeline? GetPipeline(int enchantId);

    /// <summary>
    /// All enchant options that can roll into <paramref name="slot"/>
    /// at <paramref name="enchantGrade"/> for the given pipeline.
    /// Empty list if no matches.
    /// </summary>
    IReadOnlyList<ItemEnchantOptionDbEntity> GetEnchantOptions(int enchantId, int slot, int enchantGrade);

    /// <summary>
    /// Reset-cost row: (chance, price, materials). All come from
    /// the root + Slot=-1 material rows. Returns null when the
    /// pipeline is unknown.
    /// </summary>
    (int chance, int price, IReadOnlyList<ItemEnchantMaterialDbEntity> materials)? GetResetCost(int enchantId);

    /// <summary>
    /// Per-slot cost row: (price, materials, orderIndex). Returns null
    /// when the pipeline or slot is unknown.
    /// </summary>
    (int price, IReadOnlyList<ItemEnchantMaterialDbEntity> materials, int? orderIndex)? GetSlotCost(int enchantId, int slot);

    /// <summary>Rebuild the in-memory pipeline tree.</summary>
    void Reload();
}

/// <summary>
/// In-memory shape of one pipeline. All child collections are pre-bucketed
/// for the lookups the runtime makes — no LINQ-scan per query.
/// </summary>
public sealed class ItemEnchantPipeline
{
    public int EnchantId { get; init; }
    public int MinimumRefine { get; init; }
    public int ResetChance { get; init; }
    public int ResetPrice { get; init; }
    /// <summary>Aegis names this pipeline applies to (case-insensitive set).</summary>
    public required HashSet<string> Targets { get; init; }
    /// <summary>Materials by slot. -1 = reset, 0..N = per-slot enchant.</summary>
    public required Dictionary<int, List<ItemEnchantMaterialDbEntity>> MaterialsBySlot { get; init; }
    /// <summary>Slot rows by slot index.</summary>
    public required Dictionary<int, ItemEnchantSlotDbEntity> SlotByIndex { get; init; }
    /// <summary>Options indexed by (slot, grade).</summary>
    public required Dictionary<(int slot, int grade), List<ItemEnchantOptionDbEntity>> OptionsBySlotGrade { get; init; }
}
