namespace Core.Database.Entities;

/// <summary>
/// One row of rAthena <c>db/produce_db.txt</c> — recipe parent.
/// Format (txt comma-separated):
/// <c>id, produce_item_id, item_lv, require_skill, require_skill_lv,
/// material_id[1..N], material_amount[1..N]</c>.
///
/// <para>The materials hang off <see cref="ProduceRecipeMaterialDbEntity"/>
/// keyed by (RecipeId, SlotIndex). A material with <c>Amount = 0</c> is
/// "must own but does not consume" — rAthena treats those as guide
/// items (e.g. "Marine Sphere Bottle Creation Guide").</para>
///
/// <para>Drives every <c>skill_produce_mix</c> call: Pharmacy / Genetic
/// pharmacies (AM_PHARMACY / GN_S_PHARMACY), Twilight Alchemy 1/2/3,
/// Create Deadly Poison (ASC_CDP), and Create New Poison
/// (GC_CREATENEWPOISON).</para>
/// </summary>
public class ProduceRecipeDbEntity
{
    /// <summary>Recipe row id (0..MAX_SKILL_PRODUCE_DB-1, rAthena MAX_SKILL_PRODUCE_DB=270).</summary>
    public int Id { get; set; }
    /// <summary>Item id of the produced item.</summary>
    public uint ProduceItemId { get; set; }
    /// <summary>Recipe tier (1..3 for weapons; higher tiers gate on higher pharmacy / forge skill).</summary>
    public byte ItemLv { get; set; }
    /// <summary>Required skill id (0 = no skill requirement; usually a Pharmacy / Forge skill).</summary>
    public ushort RequireSkillId { get; set; }
    /// <summary>Required skill level (≤ caster's learned level).</summary>
    public byte RequireSkillLv { get; set; }
}

/// <summary>
/// One material slot of a <see cref="ProduceRecipeDbEntity"/>.
/// Composite key (RecipeId, SlotIndex). <c>Amount = 0</c> denotes a
/// "must have in inventory but does not consume" guide item.
/// </summary>
public class ProduceRecipeMaterialDbEntity
{
    public int RecipeId { get; set; }
    public byte SlotIndex { get; set; }
    public uint MaterialItemId { get; set; }
    public uint MaterialAmount { get; set; }
}
