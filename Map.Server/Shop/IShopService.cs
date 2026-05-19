using Map.Server.Entities;
using Map.Server.Scripting.Records;

namespace Map.Server.Shop;

/// <summary>
/// NPC shop buy/sell. Port of rAthena <c>npc_buylist</c> /
/// <c>npc_selllist</c> (npc.cpp:2762 / 2997). Shop catalogs come from
/// the scripting layer (<see cref="ShopRegistration"/> seeded via
/// <c>registerShop</c>); pricing rules and zeny accounting live here.
/// </summary>
public interface IShopService
{
    /// <summary>
    /// Buy <paramref name="items"/> from <paramref name="shop"/>. Each
    /// element is (item_id, amount). Returns
    /// <see cref="ShopOpResult.Ok"/> on success.
    /// </summary>
    ShopOpResult Buy(PlayerEntity buyer, ShopRegistration shop, IReadOnlyList<(int ItemId, int Amount)> items);

    /// <summary>
    /// Sell <paramref name="items"/> from inventory to the shop. Each
    /// element is (inventory_slot, amount). NPC shops accept any item
    /// at half its buy price (rAthena <c>battle_config.sell_ratio</c>
    /// default = 50%).
    /// </summary>
    ShopOpResult Sell(PlayerEntity seller, IReadOnlyList<(int InventoryIndex, int Amount)> items);
}

/// <summary>Outcome of a buy/sell. Mirrors rAthena <c>e_purchase_result</c> trimmed.</summary>
public enum ShopOpResult
{
    Ok = 0,
    NotEnoughZeny = 1,
    InventoryFull = 2,
    NotInShop = 3,
    InvalidSlot = 4,
    InvalidQuantity = 5,
}
