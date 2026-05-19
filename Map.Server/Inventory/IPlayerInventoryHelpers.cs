using Map.Server.Entities;

namespace Map.Server.Inventory;

/// <summary>
/// Inventory-side helpers that mirror the remaining rAthena
/// <c>pc_*</c> inventory functions: cart, rentals, item cooldowns,
/// identify-all, autoloot. Same pattern as
/// <see cref="IPlayerEquipHelpers"/> — canonical entry points so
/// scripts and atcommands route through one place; deferred features
/// are documented no-ops.
/// </summary>
public interface IPlayerInventoryHelpers
{
    /// <summary>
    /// rAthena <c>pc_putitemtocart</c> (pc.cpp:9128) — move from
    /// inventory to cart. Stub until cart-state hydration on session
    /// enter lands (CartInventoryRepository exists but cart contents
    /// aren't loaded into the session yet).
    /// </summary>
    bool PutItemToCart(PlayerEntity pc, int inventoryIndex, int amount);

    /// <summary>rAthena <c>pc_cart_delitem</c> — remove a cart row.</summary>
    bool CartDelItem(PlayerEntity pc, int cartIndex, int amount);

    /// <summary>rAthena <c>pc_getitemfromcart</c> — move from cart to inventory.</summary>
    bool GetItemFromCart(PlayerEntity pc, int cartIndex, int amount);

    /// <summary>
    /// rAthena <c>pc_inventory_rental_add</c> (pc.cpp:1216) — push a
    /// rental timer for an inventory row. Stub: rental data lives on
    /// the existing InventoryItem.ExpireTime; this method becomes
    /// the canonical setter so item-effects don't write the column
    /// directly.
    /// </summary>
    void InventoryRentalAdd(PlayerEntity pc, int inventoryIndex, int seconds);

    /// <summary>rAthena <c>pc_inventory_rental_clear</c> — drop all rental timers.</summary>
    void InventoryRentalClear(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_inventory_rentals</c> (pc.cpp:1187) — tick every
    /// rental and remove expired rows. Called from the autosave loop.
    /// </summary>
    void InventoryRentalsTick(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_identifyall</c> (pc.cpp:5230) — flip every
    /// inventory row to identified. Returns the number affected.
    /// </summary>
    int IdentifyAll(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_itemcd_add</c> (pc.cpp:13357) — register an
    /// item-specific cooldown. Per-PC table keyed by item id.
    /// </summary>
    void ItemCooldownAdd(PlayerEntity pc, int itemId, int cooldownMs);

    /// <summary>
    /// rAthena <c>pc_itemcd_check</c> — true if the cooldown is still
    /// active. Used by <c>pc_useitem</c> + skill cooldowns.
    /// </summary>
    bool ItemCooldownActive(PlayerEntity pc, int itemId);

    /// <summary>
    /// rAthena <c>pc_itemcd_do</c> — set or refresh a cooldown.
    /// Cleaner name than rAthena's; same semantics.
    /// </summary>
    void ItemCooldownSet(PlayerEntity pc, int itemId, int cooldownMs);

    /// <summary>
    /// rAthena <c>pc_isautolooting</c> — true if the PC has Autoloot
    /// enabled OR the item id is whitelisted via @alootid. Stub
    /// returning false until the autoloot flag lands.
    /// </summary>
    bool IsAutolooting(PlayerEntity pc, int itemId);
}
