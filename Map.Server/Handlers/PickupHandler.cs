using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client pickup request. rAthena <c>clif_parse_TakeItem</c>: looks up the
/// floor item by entity id, validates range + map, removes the floor entity
/// and broadcasts <c>ZC_ITEM_DISAPPEAR</c>.
///
/// MS3 first slice: inventory persistence is NOT wired here. The dropped
/// item is logged; the actual <c>inventory</c> table update lands when the
/// full inventory model arrives.
/// </summary>
[PacketHandler(PacketHeader.CZ_ITEM_PICKUP)]
public class PickupHandler(
    IEntityRegistry registry,
    IItemDropService drops,
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
                logger.LogInformation(
                    "Char {CharId} picked up item {ItemId} x{Amount} (entity {EntityId})",
                    player.CharacterId, item!.ItemId, item.Amount, item.Id.Value);
                break;
            case IItemDropService.PickupResult.OutOfRange:
                logger.LogDebug(
                    "Pickup out of range: char {CharId} at ({X},{Y}) requested item entity {Entity}",
                    player.CharacterId, player.X, player.Y, packet.ItemEntityId);
                break;
            case IItemDropService.PickupResult.ItemNotFound:
            case IItemDropService.PickupResult.WrongMap:
                // Either someone else picked it up or it despawned — silent.
                break;
        }
        return Task.CompletedTask;
    }
}
