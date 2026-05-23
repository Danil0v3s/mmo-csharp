namespace Core.Database.Entities;

// ============================================================================
// DB-8h: refine + enchantgrade re-normalized
// ============================================================================

/// <summary>
/// Refine group (rAthena <c>db/re/refine.yml</c>). Each group covers
/// one equipment class (Armor, Weapon1, Weapon2, Weapon3, Weapon4,
/// Shadow_Armor, Shadow_Weapon). Per-item-level / per-refine-level
/// rate + cost data hangs off the child tables.
/// </summary>
public class RefineGroupDbEntity
{
    public string GroupName { get; set; } = string.Empty;
}

/// <summary>
/// Per-(group, item-level, refine-level) bonus + rate row.
/// Composite (GroupName, ItemLevel, RefineLevel). Bonus is the
/// stat boost gained at that refine level.
/// </summary>
public class RefineLevelDbEntity
{
    public string GroupName { get; set; } = string.Empty;
    /// <summary>Item-level bucket (1..4 for weapons, single for armor).</summary>
    public int ItemLevel { get; set; }
    /// <summary>Refine level (+1..+20).</summary>
    public int RefineLevel { get; set; }
    /// <summary>Stat bonus this refine level grants (per-group meaning).</summary>
    public int Bonus { get; set; }
}

/// <summary>
/// Per-refine-attempt chance + cost row. Composite
/// (GroupName, ItemLevel, RefineLevel, ChanceType). ChanceType is
/// one of "Normal", "HD", "Enriched", "Bsb" — different refine
/// materials with different success curves.
/// </summary>
public class RefineChanceDbEntity
{
    public string GroupName { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public int RefineLevel { get; set; }
    public string ChanceType { get; set; } = string.Empty;
    /// <summary>Success rate / 10000.</summary>
    public int Rate { get; set; }
    /// <summary>Zeny cost per attempt.</summary>
    public int Price { get; set; }
    /// <summary>Material Aegis name consumed (Elunium / HD_Elunium / etc.).</summary>
    public string MaterialAegis { get; set; } = string.Empty;
}

/// <summary>
/// Enchant-grade group (rAthena <c>db/re/enchantgrade.yml</c>). Each
/// row covers one equipment Type (Armor, Weapon1..4). Child tables
/// hold per-level + per-grade + per-refine chance.
/// </summary>
public class EnchantGradeDbEntity
{
    /// <summary>Equipment type (e.g. "Armor", "Weapon1").</summary>
    public string EquipType { get; set; } = string.Empty;
}

/// <summary>
/// Per-(EquipType, ItemLevel, Grade) row. Composite key.
/// </summary>
public class EnchantGradeLevelDbEntity
{
    public string EquipType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    /// <summary>Grade name (None, D, C, B, A).</summary>
    public string Grade { get; set; } = string.Empty;
}

/// <summary>
/// Per-refine-level success chance for an enchant grade attempt.
/// Composite (EquipType, ItemLevel, Grade, Refine).
/// </summary>
public class EnchantGradeChanceDbEntity
{
    public string EquipType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int Refine { get; set; }
    /// <summary>Success chance / 10000.</summary>
    public int Chance { get; set; }
}
