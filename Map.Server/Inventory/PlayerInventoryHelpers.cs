using System.Collections.Concurrent;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IPlayerInventoryHelpers"/>. Real implementations:
/// <list type="bullet">
///   <item>Item cooldown table keyed by (charId, itemId) — works fully.</item>
///   <item>Cart family — push / pull moves rows between
///         <c>session.Inventory</c> and <c>session.Cart</c>; persistence
///         flushes through <c>RemovedInventoryIds</c>-style tracking
///         (a follow-up commit wires the cart save path through the
///         existing autosave repo).</item>
///   <item>Rental sweep — shares the ExpireTime column on
///         <see cref="InventoryItem"/>; PlayerEquipHelpers.CheckExpiration
///         is the canonical reaper.</item>
///   <item>IdentifyAll — flips <see cref="InventoryItem.Identified"/> on
///         every row that has it false.</item>
///   <item>IsAutolooting — checks the session's autoloot whitelist + rate.</item>
/// </list>
/// </summary>
public sealed class PlayerInventoryHelpers : IPlayerInventoryHelpers
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly IItemCatalog _catalog;
    private readonly ILogger<PlayerInventoryHelpers> _logger;

    // (charId, itemId) → cooldown-ends-at tick.
    private readonly ConcurrentDictionary<(int CharId, int ItemId), long> _itemCd = new();

    public PlayerInventoryHelpers(
        ISessionManagerAccessor sessions,
        IItemCatalog catalog,
        ILogger<PlayerInventoryHelpers> logger)
    {
        _sessions = sessions;
        _catalog = catalog;
        _logger = logger;
    }

    public bool PutItemToCart(PlayerEntity pc, int inventoryIndex, int amount)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null
            || inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count
            || amount <= 0) return false;
        var src = session.Inventory[inventoryIndex];
        if (src.Equip != 0 || src.EquipSwitch != 0) return false; // can't cart equipped
        var moveCount = (uint)Math.Min(amount, (int)src.Amount);
        session.Cart ??= new List<InventoryItem>();

        // Try to merge with an existing stack of the same nameid in
        // the cart (rAthena: cart_inventory stacks like inventory).
        var existing = session.Cart.FirstOrDefault(c =>
            c.NameId == src.NameId && c.Refine == src.Refine
            && c.Card0 == src.Card0 && c.Card1 == src.Card1
            && c.Card2 == src.Card2 && c.Card3 == src.Card3);
        if (existing != null)
        {
            existing.Amount += moveCount;
        }
        else
        {
            session.Cart.Add(new InventoryItem
            {
                ServerIndex = session.Cart.Count,
                NameId = src.NameId,
                Amount = moveCount,
                Identified = src.Identified,
                Refine = src.Refine,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime,
                Bound = src.Bound,
                UniqueId = src.UniqueId,
            });
        }

        src.Amount -= moveCount;
        if (src.Amount == 0)
        {
            if (src.Id > 0) session.RemovedInventoryIds.Add(src.Id);
            session.Inventory.RemoveAt(inventoryIndex);
        }
        _logger.LogInformation(
            "pc_putitemtocart: char {Char} {Item} x{N} → cart",
            pc.CharacterId, src.NameId, moveCount);
        return true;
    }

    public bool CartDelItem(PlayerEntity pc, int cartIndex, int amount)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Cart == null
            || cartIndex < 0 || cartIndex >= session.Cart.Count
            || amount <= 0) return false;
        var row = session.Cart[cartIndex];
        var delCount = (uint)Math.Min(amount, (int)row.Amount);
        row.Amount -= delCount;
        if (row.Amount == 0) session.Cart.RemoveAt(cartIndex);
        return true;
    }

    public bool GetItemFromCart(PlayerEntity pc, int cartIndex, int amount)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Cart == null
            || cartIndex < 0 || cartIndex >= session.Cart.Count
            || amount <= 0) return false;
        session.Inventory ??= new List<InventoryItem>();
        var src = session.Cart[cartIndex];
        var moveCount = (uint)Math.Min(amount, (int)src.Amount);

        var existing = session.Inventory.FirstOrDefault(i =>
            i.NameId == src.NameId && i.Refine == src.Refine
            && i.Card0 == src.Card0 && i.Card1 == src.Card1
            && i.Card2 == src.Card2 && i.Card3 == src.Card3
            && i.Equip == 0);
        if (existing != null)
        {
            existing.Amount += moveCount;
        }
        else
        {
            session.Inventory.Add(new InventoryItem
            {
                ServerIndex = session.Inventory.Count,
                NameId = src.NameId,
                Amount = moveCount,
                Identified = src.Identified,
                Refine = src.Refine,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime,
                Bound = src.Bound,
                UniqueId = src.UniqueId,
            });
        }

        src.Amount -= moveCount;
        if (src.Amount == 0) session.Cart.RemoveAt(cartIndex);
        return true;
    }

    public void InventoryRentalAdd(PlayerEntity pc, int inventoryIndex, int seconds)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null
            || inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count) return;
        var row = session.Inventory[inventoryIndex];
        // rAthena: ExpireTime is absolute Unix seconds.
        row.ExpireTime = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds);
        _logger.LogDebug(
            "pc_inventory_rental_add: char {Char} slot {Idx} +{Sec}s",
            pc.CharacterId, inventoryIndex, seconds);
    }

    public void InventoryRentalClear(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return;
        for (var i = session.Inventory.Count - 1; i >= 0; i--)
        {
            if (session.Inventory[i].ExpireTime != 0)
            {
                if (session.Inventory[i].Id > 0) session.RemovedInventoryIds.Add(session.Inventory[i].Id);
                session.Inventory.RemoveAt(i);
            }
        }
    }

    public void InventoryRentalsTick(PlayerEntity pc)
    {
        // Shares the ExpireTime sweep with PlayerEquipHelpers.CheckExpiration.
        // Keeping a separate entry so callers using the canonical rAthena
        // name route here.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        for (var i = session.Inventory.Count - 1; i >= 0; i--)
        {
            var row = session.Inventory[i];
            if (row.ExpireTime != 0 && row.ExpireTime < now)
            {
                if (row.Id > 0) session.RemovedInventoryIds.Add(row.Id);
                session.Inventory.RemoveAt(i);
            }
        }
    }

    public int IdentifyAll(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return 0;
        var count = 0;
        foreach (var row in session.Inventory)
        {
            if (!row.Identified)
            {
                row.Identified = true;
                count++;
            }
        }
        if (count > 0)
        {
            _logger.LogInformation("pc_identifyall: char {Char} flipped {N} item(s)", pc.CharacterId, count);
        }
        return count;
    }

    public void ItemCooldownAdd(PlayerEntity pc, int itemId, int cooldownMs)
        => _itemCd[(pc.CharacterId, itemId)] = Environment.TickCount64 + cooldownMs;

    public bool ItemCooldownActive(PlayerEntity pc, int itemId)
        => _itemCd.TryGetValue((pc.CharacterId, itemId), out var ends) && ends > Environment.TickCount64;

    public void ItemCooldownSet(PlayerEntity pc, int itemId, int cooldownMs)
        => ItemCooldownAdd(pc, itemId, cooldownMs);

    public bool IsAutolooting(PlayerEntity pc, int itemId)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session == null) return false;
        // rAthena: alootid whitelist wins over the global rate.
        if (session.AutolootItemIds.Contains((uint)itemId)) return true;
        // Type-based autoloot needs the item_db type lookup.
        if (session.AutolootTypeMask != 0)
        {
            var def = _catalog.Get((uint)itemId);
            if (def?.Type != null && int.TryParse(def.Type, out var t)
                && (session.AutolootTypeMask & (1u << t)) != 0)
            {
                return true;
            }
        }
        // Global rate gate — autoloot rate is a percentage 1..100.
        if (session.AutolootRate == 0) return false;
        return Random.Shared.Next(100) < session.AutolootRate;
    }
}
