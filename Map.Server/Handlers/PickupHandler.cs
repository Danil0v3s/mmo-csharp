using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client pickup request. rAthena <c>clif_parse_TakeItem</c> →
/// <c>pc_takeitem</c>: looks up the floor item, validates range + map +
/// loot-protection, removes the floor entity (broadcasting
/// <c>ZC_ITEM_DISAPPEAR</c>), and adds the stack to the picker's
/// inventory via <see cref="IInventoryService.GiveItem"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_ITEM_PICKUP)]
public class PickupHandler(
    IEntityRegistry registry,
    IItemDropService drops,
    IInventoryService inventory,
    ILogger<PickupHandler> logger
) : IPacketHandler<MapSessionData, CZ_ITEM_PICKUP>
{
    public Task HandleAsync(MapSessionData session, CZ_ITEM_PICKUP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var result = drops.TryPickup(player, new EntityId(packet.ItemEntityId), out var item);
        switch (result)
        {
            case IItemDropService.PickupResult.Ok:
                // rAthena: pc_additem then clif_additem ack. GiveItem
                // handles the inventory-slot merge + ZC_ITEM_PICKUP_ACK
                // emission already.
                if (item != null && !inventory.GiveItem(session, (uint)item.ItemId, item.Amount))
                {
                    logger.LogWarning(
                        "Inventory full / over-weight: char {CharId} couldn't take {ItemId} x{Amount}",
                        player.CharacterId, item.ItemId, item.Amount);
                }
                else
                {
                    logger.LogInformation(
                        "Char {CharId} picked up item {ItemId} x{Amount} (entity {EntityId})",
                        player.CharacterId, item!.ItemId, item.Amount, item.Id.Value);
                }
                break;
            case IItemDropService.PickupResult.OutOfRange:
                logger.LogDebug(
                    "Pickup out of range: char {CharId} at ({X},{Y}) requested item entity {Entity}",
                    player.CharacterId, player.X, player.Y, packet.ItemEntityId);
                break;
            case IItemDropService.PickupResult.OwnerProtected:
                // Drop is inside someone else's loot-protection window.
                // rAthena silently no-ops; client retries.
                break;
            case IItemDropService.PickupResult.ItemNotFound:
            case IItemDropService.PickupResult.WrongMap:
                // Either someone else picked it up or it despawned — silent.
                break;
        }
        return Task.CompletedTask;
    }
}
