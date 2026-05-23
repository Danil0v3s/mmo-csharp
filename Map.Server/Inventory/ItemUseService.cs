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
    private readonly IItemHookDispatcher? _hookDispatcher;
    private readonly ILogger<ItemUseService> _logger;

    public ItemUseService(
        IItemCatalog catalog,
        ItemEffectRegistry effects,
        Status.ISessionManagerAccessor sessions,
        ILogger<ItemUseService> logger,
        IItemHookDispatcher? hookDispatcher = null)
    {
        _catalog = catalog;
        _effects = effects;
        _sessions = sessions;
        _hookDispatcher = hookDispatcher;
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

        // CONV-4: try the TS-registered onUse hook first. When a registration
        // exists, the dispatcher owns the invocation (sync stack decrement
        // still runs below since rAthena's item-use semantics consume one
        // stack on success regardless of whether the script does anything
        // visible). When no hook is registered, fall back to the legacy
        // C# ItemEffectRegistry handler — the converter only emits TS
        // hooks for items with a SQL `script` column, so non-scripted
        // items (rare) still need the C# fallback.
        var dispatched = _hookDispatcher?.TryInvokeOnUse(session, user, item) ?? false;
        if (!dispatched)
        {
            var handler = _effects.Get(row.NameAegis);
            if (handler == null)
            {
                _logger.LogDebug("ItemUseService: no handler or onUse hook for aegis '{Aegis}'", row.NameAegis);
                return false;
            }
            if (!handler.Apply(user)) return false;
        }

        // Decrement stack; remove slot if empty (rAthena pc_delitem).
        item.Amount -= 1;
        if (item.Amount <= 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.RemoveAt(slotIndex);
        }
        _logger.LogDebug(
            "Char {Char} used item {Aegis} (slot {Slot}, path={Path}); {Remaining} left",
            user.CharacterId, row.NameAegis, slotIndex,
            dispatched ? "ts-onUse" : "c#-handler", item.Amount);
        return true;
    }
}
