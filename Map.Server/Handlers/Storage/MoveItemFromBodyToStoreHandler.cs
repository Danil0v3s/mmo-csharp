using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Storage;

namespace Map.Server.Handlers.Storage;

/// <summary>
/// Inventory → storage move. rAthena
/// <c>clif_parse_MoveToKafra</c> (clif.cpp:13595). Drives
/// <see cref="IStorageService.AddFromInventory"/>, then emits
/// <c>ZC_ADD_ITEM_TO_STORE</c> for the destination slot +
/// <c>ZC_NOTIFY_STOREITEM_COUNTINFO</c> so the client UI's counter
/// updates.
/// </summary>
[PacketHandler(PacketHeader.CZ_MOVE_ITEM_FROM_BODY_TO_STORE)]
public class MoveItemFromBodyToStoreHandler(
    IEntityRegistry registry,
    IStorageService storage,
    ILogger<MoveItemFromBodyToStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_MOVE_ITEM_FROM_BODY_TO_STORE>
{
    public Task HandleAsync(MapSessionData session, CZ_MOVE_ITEM_FROM_BODY_TO_STORE packet)
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

        var serverIndex = packet.ClientIndex - 2;
        var result = storage.AddFromInventory(session, serverIndex, packet.Amount);
        if (result != StorageOpResult.Ok)
        {
            logger.LogDebug(
                "Storage AddFromInventory failed: char {Char} slot {Slot}: {Reason}",
                player.CharacterId, serverIndex, result);
            return Task.CompletedTask;
        }

        // The destination storage slot is the last one if a new slot was
        // appended, or the existing matching slot otherwise. AddFromInventory
        // mutates storage in place; we look up the item with this NameId
        // that just absorbed the amount.
        var dstIndex = stor.Items.Count - 1;
        // If the dest slot's amount is exactly the move amount, it was a
        // new entry; otherwise the merge target is at some earlier index.
        // For the wire packet we send the resulting slot — find it by
        // scanning for matching nameid.
        for (var i = 0; i < stor.Items.Count; i++)
        {
            if (stor.Items[i].NameId == 0) continue;
            dstIndex = i;
            // Take the LAST matching entry (a fresh append wins over an older
            // merge target if the stack went over the cap and split).
        }
        var dst = stor.Items[dstIndex];
        session.EnqueuePacket(StorageNotifier.BuildAdd(dstIndex, packet.Amount, dst));
        session.EnqueuePacket(StorageNotifier.BuildCount(stor));
        return Task.CompletedTask;
    }
}
