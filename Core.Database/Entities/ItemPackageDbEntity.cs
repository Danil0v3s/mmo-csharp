namespace Core.Database.Entities;

/// <summary>
/// Item package definition (rAthena <c>db/re/item_packages.yml</c>).
/// One row per opener item (e.g. Select_Example1, Gift_Box). Entries
/// — the items inside the package — live in
/// <see cref="ItemPackageEntryDbEntity"/>.
///
/// rAthena yml nests entries under <c>Groups[].Items[]</c>; we flatten
/// Group into the entry row's <c>GroupId</c> column.
///
/// DB-8b wave replaces the prior <c>ItemPackagesEntity :
/// PayloadStringKeyEntity</c> JSON blob.
/// </summary>
public class ItemPackageDbEntity
{
    /// <summary>Opener item Aegis name (the item that gets consumed to open the package).</summary>
    public string ItemAegis { get; set; } = string.Empty;
}

/// <summary>
/// One item inside an <see cref="ItemPackageDbEntity"/>. Composite
/// key (ItemAegis, GroupId, ContainedItemAegis). When opening, the
/// runtime grants one item per Group (each Group's entries are
/// alternatives the user picks from or a guaranteed bundle).
/// </summary>
public class ItemPackageEntryDbEntity
{
    /// <summary>FK to <see cref="ItemPackageDbEntity.ItemAegis"/>.</summary>
    public string ItemAegis { get; set; } = string.Empty;

    /// <summary>
    /// Group ordinal inside the package (0..N). Each group's entries
    /// are picked from independently — group 0 might be a guaranteed
    /// refine, group 1 a stack of potions, etc.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>Granted item Aegis name.</summary>
    public string ContainedItemAegis { get; set; } = string.Empty;

    /// <summary>Amount granted (default 1).</summary>
    public int Amount { get; set; } = 1;

    /// <summary>Refine level if pre-refined.</summary>
    public int? Refine { get; set; }

    /// <summary>Rental hours if time-bound.</summary>
    public int? RentalHours { get; set; }

    /// <summary>Random option group name.</summary>
    public string? RandomOptionGroup { get; set; }
}
