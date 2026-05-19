using Map.Server.Entities;
using Map.Server.Inventory.ItemEffects;
using Map.Server.Items;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// <see cref="IItemUseService"/>. Looks up the item row in the
/// catalog, dispatches to an <see cref="IItemEffectHandler"/> in
/// <see cref="ItemEffectRegistry"/> (strategy pattern — same shape as
/// SkillUnitService.SpecFor), and decrements the stack on success.
///
/// Adding a new consumable: implement <see cref="IItemEffectHandler"/>
/// and register it in <see cref="ItemEffectRegistry"/>. No switch case
/// in this service to touch.
/// </summary>
public sealed class ItemUseService : IItemUseService
{
    private readonly IItemCatalog _catalog;
    private readonly ItemEffectRegistry _effects;
    private readonly Status.ISessionManagerAccessor _sessions;
    private readonly ILogger<ItemUseService> _logger;

    public ItemUseService(
        IItemCatalog catalog,
        ItemEffectRegistry effects,
        Status.ISessionManagerAccessor sessions,
        ILogger<ItemUseService> logger)
    {
        _catalog = catalog;
        _effects = effects;
        _sessions = sessions;
        _logger = logger;
    }

    public bool UseItem(PlayerEntity user, int slotIndex)
    {
        var session = _sessions.GetByEntityId(user.Id);
        if (session?.Inventory is not { } inv) return false;
        if (slotIndex < 0 || slotIndex >= inv.Count) return false;
        var item = inv[slotIndex];
        if (item.Amount <= 0) return false;

        var row = _catalog.Get(item.NameId);
        if (row == null) return false;

        var handler = _effects.Get(row.NameAegis);
        if (handler == null)
        {
            _logger.LogDebug("ItemUseService: no handler for aegis '{Aegis}'", row.NameAegis);
            return false;
        }
        if (!handler.Apply(user)) return false;

        // Decrement stack; remove slot if empty (rAthena pc_delitem).
        item.Amount -= 1;
        if (item.Amount <= 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.RemoveAt(slotIndex);
        }
        _logger.LogDebug(
            "Char {Char} used item {Aegis} (slot {Slot}); {Remaining} left",
            user.CharacterId, row.NameAegis, slotIndex, item.Amount);
        return true;
    }
}
