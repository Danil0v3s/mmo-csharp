namespace Core.Database.Entities;

// ============================================================================
// DB-8g — enchant pipeline catalogs. Replaces 5 PayloadJson catalogs:
// item_enchant / item_reform / laphine_synthesis / laphine_upgrade /
// item_randomopt_group. Each is the deeply-nested recipe shape rAthena
// uses for equipment enchantment, refresh, and Lapine workflows.
// ============================================================================

/// <summary>
/// Item enchant pipeline (rAthena <c>db/re/item_enchant.yml</c>).
/// One root row per enchant pipeline id, with target items + reset
/// recipe + per-slot enchant options as child tables.
/// </summary>
public class ItemEnchantDbEntity
{
    public int EnchantId { get; set; }
    public int MinimumRefine { get; set; }
    /// <summary>Reset.Chance (0..100000).</summary>
    public int ResetChance { get; set; }
    /// <summary>Reset.Price in zeny.</summary>
    public int ResetPrice { get; set; }
}

/// <summary>Items targetable by an enchant pipeline. Composite (EnchantId, ItemAegis).</summary>
public class ItemEnchantTargetDbEntity
{
    public int EnchantId { get; set; }
    public string ItemAegis { get; set; } = string.Empty;
}

/// <summary>
/// Material consumed by an enchant pipeline. Composite (EnchantId, Slot, MaterialAegis).
/// Slot=-1 → Reset materials; Slot 0..3 → per-slot enchant materials.
/// </summary>
public class ItemEnchantMaterialDbEntity
{
    public int EnchantId { get; set; }
    /// <summary>-1 = Reset; 0..N = per-slot enchant.</summary>
    public int Slot { get; set; }
    public string MaterialAegis { get; set; } = string.Empty;
    public int Amount { get; set; }
}

/// <summary>Per-slot enchant cost + order. Composite (EnchantId, Slot).</summary>
public class ItemEnchantSlotDbEntity
{
    public int EnchantId { get; set; }
    public int Slot { get; set; }
    public int Price { get; set; }
    /// <summary>Order of slot rolls — rAthena <c>Order: [{ Slot: 3 }, ...]</c> index.</summary>
    public int? OrderIndex { get; set; }
}

/// <summary>
/// One enchant option a slot can roll into. Composite
/// (EnchantId, Slot, EnchantGrade, OptionItemAegis). The roll picks
/// uniformly from rows that match (slot, grade) for the enchant.
/// </summary>
public class ItemEnchantOptionDbEntity
{
    public int EnchantId { get; set; }
    public int Slot { get; set; }
    public int EnchantGrade { get; set; }
    public string OptionItemAegis { get; set; } = string.Empty;
    /// <summary>Optional per-row chance weight (rAthena Chance, null → uniform).</summary>
    public int? Chance { get; set; }
}

/// <summary>
/// Item reform recipe (rAthena <c>db/re/item_reform.yml</c>). Each
/// root row is one resulting item; <see cref="ItemReformBaseDbEntity"/>
/// holds the base-item alternatives that reform into it.
/// </summary>
public class ItemReformDbEntity
{
    /// <summary>Result item Aegis (the item produced by the reform).</summary>
    public string ResultItemAegis { get; set; } = string.Empty;
}

/// <summary>
/// One base-item alternative for an <see cref="ItemReformDbEntity"/>.
/// Composite (ResultItemAegis, BaseItemAegis). Carries the reform's
/// constraints + outputs.
/// </summary>
public class ItemReformBaseDbEntity
{
    public string ResultItemAegis { get; set; } = string.Empty;
    public string BaseItemAegis { get; set; } = string.Empty;
    public int? MaximumRefine { get; set; }
    public int? ChangeRefine { get; set; }
    public string? ResultItemOverride { get; set; }
    public string? RandomOptionGroup { get; set; }
    public bool ClearSlots { get; set; }
    public bool RemoveEnchantgrade { get; set; }
    public bool CardsAllowed { get; set; } = true;
}

/// <summary>
/// Lapine synthesis recipe (rAthena <c>db/re/laphine_synthesis.yml</c>).
/// </summary>
public class LaphineSynthesisDbEntity
{
    /// <summary>Recipe item (the consumable that opens the synthesis UI).</summary>
    public string RecipeItem { get; set; } = string.Empty;
    public string? RewardGroup { get; set; }
    public int RequiredRequirementsCount { get; set; }
}

/// <summary>One required ingredient for a <see cref="LaphineSynthesisDbEntity"/>.</summary>
public class LaphineSynthesisRequirementDbEntity
{
    public string RecipeItem { get; set; } = string.Empty;
    public string RequirementItem { get; set; } = string.Empty;
    public int? RefineMin { get; set; }
    public int? RefineMax { get; set; }
}

/// <summary>
/// Lapine upgrade recipe (rAthena <c>db/re/laphine_upgrade.yml</c>).
/// </summary>
public class LaphineUpgradeDbEntity
{
    /// <summary>Upgrade item (the consumable that opens the upgrade UI).</summary>
    public string UpgradeItem { get; set; } = string.Empty;
    public int? RandomOptionGroup { get; set; }
    public int? MinimumRefine { get; set; }
}

/// <summary>
/// One target item the upgrade can apply to. Composite
/// (UpgradeItem, TargetItem).
/// </summary>
public class LaphineUpgradeTargetDbEntity
{
    public string UpgradeItem { get; set; } = string.Empty;
    public string TargetItem { get; set; } = string.Empty;
}

/// <summary>
/// Random-opt group definition (rAthena
/// <c>db/re/item_randomopt_group.yml</c>). Each group is a named
/// bundle of random options assigned at item creation.
/// </summary>
public class ItemRandomOptGroupDbEntity
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
}

/// <summary>
/// One random option a slot can roll. Composite
/// (GroupId, Slot, OptionName). The slot picks one option per roll;
/// the option's MinValue..MaxValue range is rolled inclusively, and
/// Chance / 10000 picks whether the option fires at all.
/// </summary>
public class ItemRandomOptGroupOptionDbEntity
{
    public int GroupId { get; set; }
    public int Slot { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    /// <summary>Probability / 10000 (5000 = 50%).</summary>
    public int Chance { get; set; }
}
