namespace Core.Database.Entities;

/// <summary>
/// One stylist option row (rAthena <c>db/re/stylist.yml</c> →
/// <see cref="StylistDbEntity"/>). Stylist NPCs read this catalog to
/// list available hairstyle / hair-color / clothes-color / cloth /
/// body / dye palette options the player can swap to, plus the
/// per-option zeny cost and required item.
///
/// Composite key on (<see cref="Look"/>, <see cref="ClientIndex"/>).
/// <c>Look</c> mirrors rAthena <c>enum look</c> (LOOK_HAIR=1,
/// LOOK_HAIR_COLOR=6, LOOK_CLOTHES_COLOR=7, LOOK_BODY=13, etc.).
/// AT-G wave added this entity — DB-1..6 skipped stylist.yml entirely.
/// </summary>
public class StylistDbEntity
{
    /// <summary>
    /// rAthena <c>enum look</c> id. Maps the option to the appearance
    /// slot it changes (1=hair, 6=hair color, 7=clothes color, 13=body).
    /// </summary>
    public int Look { get; set; }

    /// <summary>
    /// Client-side option index. The stylist UI sends this back to the
    /// server when a player picks an option; the server validates the
    /// (Look, ClientIndex) tuple against this catalog.
    /// </summary>
    public int ClientIndex { get; set; }

    /// <summary>
    /// New look value to apply (e.g. style id for hair, palette id for
    /// hair-color). Same numeric space as rAthena's <c>look</c> enum.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// True if this option targets the Doram race exclusively.
    /// rAthena splits the cost table into <c>CostsHuman</c> /
    /// <c>CostsDoram</c>; we collapse into one row per race-variant.
    /// </summary>
    public bool DoramOnly { get; set; }

    /// <summary>Zeny cost to apply this option (0 if free).</summary>
    public int CostZeny { get; set; }

    /// <summary>
    /// Required item Aegis name (e.g. "Hairstyle_Coupon"); null if no
    /// item gate. Stylist consumes one of these on apply.
    /// </summary>
    public string? RequiredItemAegis { get; set; }

    /// <summary>
    /// Required item-box Aegis name. When set, the stylist accepts the
    /// box in lieu of the loose item and opens it post-apply.
    /// </summary>
    public string? RequiredItemBoxAegis { get; set; }
}
