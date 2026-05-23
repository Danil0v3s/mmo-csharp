namespace Map.Server.Inventory;

/// <summary>
/// DBR-2g: item package / gift-box pipeline. Port of rAthena
/// <c>itemdb_package_open</c> (db/re/item_packages.yml).
///
/// <para>
/// When a player uses a package opener item (e.g. "Select_Example1"
/// or a "Gift_Box"), the runtime grants the contained items per
/// group. rAthena default: every group's entries are granted in
/// full (a deterministic bundle). Some packages let the UI pick one
/// per group; that's a per-package nuance the consumer handles.
/// </para>
/// </summary>
public interface IItemPackageService
{
    /// <summary>
    /// Get the package contents for an opener item. Returns null if
    /// the item is not a package opener. The returned groups are
    /// ordered by group id; entries inside each group preserve
    /// rAthena yml ordering.
    /// </summary>
    ItemPackageContents? GetContents(string openerItemAegis);

    /// <summary>Diagnostics: number of packages in the catalog.</summary>
    int CatalogCount { get; }
}

/// <summary>Full contents of a package, grouped by GroupId.</summary>
public sealed record ItemPackageContents(
    string OpenerAegis,
    IReadOnlyList<ItemPackageGroup> Groups);

/// <summary>One group of items inside a package.</summary>
public sealed record ItemPackageGroup(
    int GroupId,
    IReadOnlyList<ItemPackageEntry> Entries);

/// <summary>One granted item per group.</summary>
public sealed record ItemPackageEntry(
    string ItemAegis,
    int Amount,
    int? Refine,
    int? RentalHours,
    string? RandomOptionGroup);
