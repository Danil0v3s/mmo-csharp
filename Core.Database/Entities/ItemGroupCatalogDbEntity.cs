namespace Core.Database.Entities;

/// <summary>
/// Item group catalog (rAthena <c>db/re/item_group_db.yml</c>). One
/// row per random-bag group name (e.g. 2013_RWC_SCROLL, Bloody_Branch).
/// Entries live in <see cref="ItemGroupCatalogEntryDbEntity"/>; the
/// rAthena yml nests entries under SubGroup (typically subgroup 1)
/// — we flatten that into the entry row's <c>SubGroup</c> column to
/// keep the SQL queryable.
///
/// DB-8b wave replaces the prior <c>ItemGroupDbEntity :
/// PayloadStringKeyEntity</c> JSON blob. Renamed to
/// ItemGroupCatalog* to avoid colliding with runtime
/// ItemGroupDbEntity (which itself was the payload type).
/// </summary>
public class ItemGroupCatalogDbEntity
{
    /// <summary>Group key (e.g. "Bloody_Branch", "2013_RWC_SCROLL").</summary>
    public string GroupName { get; set; } = string.Empty;
}

/// <summary>
/// One item entry inside an <see cref="ItemGroupCatalogDbEntity"/>.
/// Composite key (GroupName, SubGroup, Index). The runtime picks a
/// random entry per SubGroup roll.
/// </summary>
public class ItemGroupCatalogEntryDbEntity
{
    /// <summary>FK to <see cref="ItemGroupCatalogDbEntity.GroupName"/>.</summary>
    public string GroupName { get; set; } = string.Empty;
    /// <summary>SubGroup index (rAthena groups multiple "guarantee rolls").</summary>
    public int SubGroup { get; set; }
    /// <summary>Per-subgroup ordinal index.</summary>
    public int Index { get; set; }
    /// <summary>Item Aegis name.</summary>
    public string ItemAegis { get; set; } = string.Empty;
    /// <summary>Weight inside the subgroup (1..N).</summary>
    public int Rate { get; set; }
    /// <summary>Server-broadcasts the recipient on drop if true (rare items).</summary>
    public bool Announced { get; set; }
    /// <summary>Amount granted (default 1).</summary>
    public int Amount { get; set; } = 1;
    /// <summary>Item duration in hours (rentals).</summary>
    public int? DurationHours { get; set; }
    /// <summary>Pre-refined level if granted refined.</summary>
    public int? Refine { get; set; }
    /// <summary>Random option group name (rAthena <c>RandomOptionGroup</c>).</summary>
    public string? RandomOptionGroup { get; set; }
}
