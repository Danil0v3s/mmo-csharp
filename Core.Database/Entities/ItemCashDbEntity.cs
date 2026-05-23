namespace Core.Database.Entities;

/// <summary>
/// Cash-shop tab (rAthena <c>db/item_cash.yml</c>). Each row is one
/// tab in the cash shop UI (New, Hot, Limited, Rental, Permanent,
/// Scrolls, Consumables, Other, Sale). Items per tab live in
/// <see cref="ItemCashEntryDbEntity"/>.
///
/// DB-8b wave replaces the prior <c>ItemCashEntity :
/// PayloadRowIndexEntity</c> JSON blob.
/// </summary>
public class ItemCashDbEntity
{
    /// <summary>Tab name (e.g. "New", "Hot", "Sale").</summary>
    public string Tab { get; set; } = string.Empty;
}

/// <summary>
/// One item listed under a cash-shop <see cref="ItemCashDbEntity"/>
/// tab. Composite key (Tab, ItemAegis).
/// </summary>
public class ItemCashEntryDbEntity
{
    /// <summary>FK to <see cref="ItemCashDbEntity.Tab"/>.</summary>
    public string Tab { get; set; } = string.Empty;

    /// <summary>Item Aegis name.</summary>
    public string ItemAegis { get; set; } = string.Empty;

    /// <summary>Price in cash points (#CASHPOINTS).</summary>
    public int Price { get; set; }
}
