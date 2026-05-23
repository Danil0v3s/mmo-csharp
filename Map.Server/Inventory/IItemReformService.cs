namespace Map.Server.Inventory;

/// <summary>
/// DBR-2f: item reform pipeline. Port of rAthena
/// <c>item_reform</c> (db/re/item_reform.yml).
///
/// <para>
/// Reform UI replaces a base item with a result item, optionally with
/// constraints (min/max refine, materials, allow cards). The opener
/// item is consumed; the base item is transformed to result.
/// </para>
/// </summary>
public interface IItemReformService
{
    /// <summary>
    /// Look up the reform configuration for an opener item (the item
    /// the player uses to open the Reform UI). Returns null if the
    /// item is not a reform trigger.
    /// </summary>
    ItemReformConfig? GetReformConfig(string openerItemAegis);

    /// <summary>Diagnostics: number of reform pipelines in the catalog.</summary>
    int CatalogCount { get; }
}

/// <summary>
/// Reform pipeline metadata. Each <see cref="ItemReformBase"/> is a
/// candidate base item the opener can transform into the result.
/// </summary>
public sealed record ItemReformConfig(
    string ResultItemAegis,
    IReadOnlyList<ItemReformBase> Bases);

/// <summary>
/// One base-item variant for an <see cref="ItemReformConfig"/>.
/// Carries the constraints + optional overrides per rAthena yml.
/// </summary>
public sealed record ItemReformBase(
    string BaseItemAegis,
    int? MaximumRefine,
    int? ChangeRefine,
    string? ResultItemOverride,
    string? RandomOptionGroup,
    bool ClearSlots,
    bool RemoveEnchantgrade,
    bool CardsAllowed);
