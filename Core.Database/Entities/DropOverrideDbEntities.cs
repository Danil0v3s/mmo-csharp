namespace Core.Database.Entities;

// ============================================================================
// DB-8i: drop override catalogs re-normalized
// ============================================================================

/// <summary>
/// Map-level drop override (rAthena <c>db/map_drops.yml</c>). Each
/// row is one map name; per-map global drops + per-mob-filtered drops
/// hang off <see cref="MapDropEntryDbEntity"/>.
/// </summary>
public class MapDropDbEntity
{
    public string MapName { get; set; } = string.Empty;
}

/// <summary>
/// One drop row attached to a <see cref="MapDropDbEntity"/>. Composite
/// (MapName, EntryIndex). GlobalDrops fire for all monsters on the
/// map; per-monster overrides set <see cref="MobFilterAegis"/> to the
/// monster's Aegis name.
/// </summary>
public class MapDropEntryDbEntity
{
    public string MapName { get; set; } = string.Empty;
    public int EntryIndex { get; set; }
    public string ItemAegis { get; set; } = string.Empty;
    /// <summary>Drop rate / 100000.</summary>
    public int Rate { get; set; }
    /// <summary>Null = applies to all monsters; set = only this mob.</summary>
    public string? MobFilterAegis { get; set; }
}

/// <summary>
/// Per-item drop rate adjustment (rAthena <c>db/mob_item_ratio.yml</c>).
/// One row per item Aegis with a global ratio modifier; optional
/// mob list filter narrows the adjustment to a subset of monsters.
/// </summary>
public class MobItemRatioDbEntity
{
    public string ItemAegis { get; set; } = string.Empty;
    /// <summary>Multiplier applied to the item's drop rate (basis-points).</summary>
    public int Ratio { get; set; }
}

/// <summary>
/// Mob filter for a <see cref="MobItemRatioDbEntity"/>. Composite
/// (ItemAegis, MobAegis). When empty, the ratio applies to all
/// monsters that drop the item.
/// </summary>
public class MobItemRatioMobDbEntity
{
    public string ItemAegis { get; set; } = string.Empty;
    public string MobAegis { get; set; } = string.Empty;
}
