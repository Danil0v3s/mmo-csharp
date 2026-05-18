namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerShop({ ... })</c> call. Variant shape — the <see cref="Kind"/>
/// discriminator selects which optional fields are populated:
/// <list type="bullet">
///   <item><c>shop</c> / <c>cash</c>: <c>Items</c> only</item>
///   <item><c>item</c>:  <c>CostItem</c> + <c>Items</c></item>
///   <item><c>point</c>: <c>CostVariable</c> + <c>Items</c></item>
///   <item><c>market</c>: <c>Items</c> with stock counts</item>
/// </list>
/// </summary>
public sealed record ShopRegistration
{
    public required ShopKind Kind { get; init; }
    public required string Map { get; init; }
    public required short X { get; init; }
    public required short Y { get; init; }
    public byte Dir { get; init; }
    public required int Sprite { get; init; }
    public required string Name { get; init; }

    public int? CostItem { get; init; }
    public string? CostVariable { get; init; }
    public required IReadOnlyList<ShopItem> Items { get; init; }
}

public enum ShopKind { Shop, Cash, Item, Point, Market }

public sealed record ShopItem(int ItemId, int Price, int? Discount = null, int? Stock = null);
