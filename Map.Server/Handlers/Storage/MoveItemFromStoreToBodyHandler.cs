using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Storage;

namespace Map.Server.Handlers.Storage;

/// <summary>
/// Storage → inventory move. rAthena
/// <c>clif_parse_MoveFromKafra</c> (clif.cpp:13627). Drives
/// <see cref="IStorageService.TakeToInventory"/>, then emits
/// <c>ZC_DELETE_ITEM_FROM_STORE</c> + <c>ZC_NOTIFY_STOREITEM_COUNTINFO</c>.
/// </summary>
[PacketHandler(PacketHeader.CZ_MOVE_ITEM_FROM_STORE_TO_BODY)]
public class MoveItemFromStoreToBodyHandler(
    IEntityRegistry registry,
    IStorageService storage,
    ILogger<MoveItemFromStoreToBodyHandler> logger
) : IPacketHandler<MapSessionData, CZ_MOVE_ITEM_FROM_STORE_TO_BODY>
{
    public Task HandleAsync(MapSessionData session, CZ_MOVE_ITEM_FROM_STORE_TO_BODY packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }
        if (session.Storage is not { IsOpen: true } stor)
        {
            return Task.CompletedTask;
        }

        var serverIndex = packet.ClientIndex - 1;
        var result = storage.TakeToInventory(session, serverIndex, packet.Amount);
        if (result != StorageOpResult.Ok)
        {
            logger.LogDebug(
                "Storage TakeToInventory failed: char {Char} slot {Slot}: {Reason}",
                player.CharacterId, serverIndex, result);
            return Task.CompletedTask;
        }

        // Use the request's serverIndex for the delete packet — rAthena
        // emits the delete for the slot the player asked about, even when
        // the underlying compaction moves entries around.
        session.EnqueuePacket(StorageNotifier.BuildDelete(serverIndex, packet.Amount));
        session.EnqueuePacket(StorageNotifier.BuildCount(stor));
        return Task.CompletedTask;
    }
}
