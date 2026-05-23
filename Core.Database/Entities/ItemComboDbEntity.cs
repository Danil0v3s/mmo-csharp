namespace Core.Database.Entities;

/// <summary>
/// Equipment-combo bonus (rAthena <c>db/re/item_combos.yml</c>). Each
/// combo is a set of item Aegis names that — when equipped together
/// — fires a bonus script (e.g. "Dragon_Slayer + Dragon_Breath →
/// +5% race-Dragon damage").
///
/// rAthena yml groups multiple combos sharing the same Script under
/// one <c>Combos:</c> array; we denormalize per-combo so each
/// <see cref="ItemComboDbEntity"/> carries its own copy of the script
/// (small overhead, queryable).
///
/// Members (which items participate) live in
/// <see cref="ItemComboMemberDbEntity"/>. DB-8b wave replaces the
/// prior <c>ItemCombosEntity : PayloadRowIndexEntity</c> JSON blob.
/// </summary>
public class ItemComboDbEntity
{
    /// <summary>Surrogate auto-id (one per combo).</summary>
    public int ComboId { get; set; }

    /// <summary>
    /// Bonus script body — the rAthena script text that runs while
    /// every member item is equipped. Multi-statement bodies are
    /// stored as a single text blob (script engine parses on apply).
    /// </summary>
    public string Script { get; set; } = string.Empty;
}

/// <summary>
/// One member-item of an <see cref="ItemComboDbEntity"/>. Composite
/// key (ComboId, MemberItemAegis). The bonus fires only when every
/// MemberItemAegis row for the combo is equipped simultaneously.
/// </summary>
public class ItemComboMemberDbEntity
{
    /// <summary>FK to <see cref="ItemComboDbEntity.ComboId"/>.</summary>
    public int ComboId { get; set; }

    /// <summary>Required item Aegis (must be equipped for combo to fire).</summary>
    public string MemberItemAegis { get; set; } = string.Empty;
}
