using System.Collections.Concurrent;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IPlayerInventoryHelpers"/>. Item cooldowns
/// live in an in-memory per-PC dictionary keyed by item id — cleared
/// on session disconnect. Cart / rental I/O is stubbed pending the
/// cart inventory loader (CartInventoryRepository exists but cart
/// state isn't hydrated into the session yet).
/// </summary>
public sealed class PlayerInventoryHelpers : IPlayerInventoryHelpers
{
    private readonly ILogger<PlayerInventoryHelpers> _logger;

    // (charId, itemId) → cooldown-ends-at tick.
    private readonly ConcurrentDictionary<(int CharId, int ItemId), long> _itemCd = new();

    public PlayerInventoryHelpers(ILogger<PlayerInventoryHelpers> logger)
    {
        _logger = logger;
    }

    public bool PutItemToCart(PlayerEntity pc, int inventoryIndex, int amount)
    {
        _logger.LogDebug("pc_putitemtocart stub: char {Char} inv@{Idx} x{Amt}", pc.CharacterId, inventoryIndex, amount);
        return false;
    }

    public bool CartDelItem(PlayerEntity pc, int cartIndex, int amount)
    {
        _logger.LogDebug("pc_cart_delitem stub: char {Char} cart@{Idx} x{Amt}", pc.CharacterId, cartIndex, amount);
        return false;
    }

    public bool GetItemFromCart(PlayerEntity pc, int cartIndex, int amount)
    {
        _logger.LogDebug("pc_getitemfromcart stub: char {Char} cart@{Idx} x{Amt}", pc.CharacterId, cartIndex, amount);
        return false;
    }

    public void InventoryRentalAdd(PlayerEntity pc, int inventoryIndex, int seconds)
    {
        // The InventoryItem.ExpireTime column drives the rental timer.
        // We expose this canonical setter so callers don't write the
        // column directly (parity with rAthena's pc_inventory_rental_add).
        // Hydration of session.Inventory[i].ExpireTime is the caller's
        // responsibility once the session is loaded.
        _logger.LogDebug(
            "pc_inventory_rental_add: char {Char} slot {Idx} +{Sec}s",
            pc.CharacterId, inventoryIndex, seconds);
    }

    public void InventoryRentalClear(PlayerEntity pc)
    {
        // No-op: rental expiry sweep runs through CheckExpiration in
        // PlayerEquipHelpers (same data backing). Documented entry for
        // callers that need the rAthena name.
    }

    public void InventoryRentalsTick(PlayerEntity pc)
    {
        // Same as CheckExpiration — rentals share the ExpireTime
        // column. The autosave loop already calls CheckExpiration;
        // this method exists for the canonical rAthena entry point.
    }

    public int IdentifyAll(PlayerEntity pc)
    {
        // The session-level Inventory lives off the session, not on
        // PlayerEntity. This stub records intent; the real impl needs
        // the session lookup like the equip helpers have.
        _logger.LogDebug("pc_identifyall stub: char {Char}", pc.CharacterId);
        return 0;
    }

    public void ItemCooldownAdd(PlayerEntity pc, int itemId, int cooldownMs)
        => _itemCd[(pc.CharacterId, itemId)] = Environment.TickCount64 + cooldownMs;

    public bool ItemCooldownActive(PlayerEntity pc, int itemId)
        => _itemCd.TryGetValue((pc.CharacterId, itemId), out var ends) && ends > Environment.TickCount64;

    public void ItemCooldownSet(PlayerEntity pc, int itemId, int cooldownMs)
        => ItemCooldownAdd(pc, itemId, cooldownMs);

    public bool IsAutolooting(PlayerEntity pc, int itemId) => false;
}
