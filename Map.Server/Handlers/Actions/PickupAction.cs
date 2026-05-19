using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Handlers.Actions;

/// <summary>
/// rAthena <c>DMG_PICKUP_ITEM</c> (action=1, clif.cpp:11671). Some
/// clients route the loot pickup through <c>CZ_REQUEST_ACTION</c> with
/// the targeted floor-item id rather than emitting a dedicated
/// <c>CZ_ITEM_PICKUP</c>. Functionally identical to
/// <see cref="PickupHandler"/> — same drop lookup, same range / owner
/// gates, same inventory delivery.
/// </summary>
public sealed class PickupAction : IActionHandler
{
    public byte ActionCode => 1;

    private readonly IEntityRegistry _entities;
    private readonly IItemDropService _drops;
    private readonly IInventoryService _inventory;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<PickupAction> _logger;

    public PickupAction(
        IEntityRegistry entities,
        IItemDropService drops,
        IInventoryService inventory,
        ISessionManagerAccessor sessions,
        ILogger<PickupAction> logger)
    {
        _entities = entities;
        _drops = drops;
        _inventory = inventory;
        _sessions = sessions;
        _logger = logger;
    }

    public void Apply(PlayerEntity player, int targetId)
    {
        if (targetId <= 0) return;

        var result = _drops.TryPickup(player, new EntityId(targetId), out var item);
        if (result != IItemDropService.PickupResult.Ok || item == null) return;

        var session = _sessions.GetByEntityId(player.Id);
        if (session == null) return;

        if (!_inventory.GiveItem(session, (uint)item.ItemId, item.Amount))
        {
            _logger.LogWarning(
                "Inventory full / over-weight: char {CharId} couldn't take {ItemId} x{Amount}",
                player.CharacterId, item.ItemId, item.Amount);
        }
    }
}
