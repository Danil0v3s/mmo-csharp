using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.World;

namespace Map.Server.Handlers.Inventory;

/// <summary>
/// Player asks to drop an inventory item onto the floor. rAthena
/// <c>clif_parse_DropItem</c> (clif.cpp:12016) → <c>pc_dropitem</c>.
/// Validates the slot + amount, refuses if equipped / trading / on a
/// nodrop map, decrements the inventory stack, spawns a floor entity
/// via <see cref="IItemDropService.DropOnFloor"/>, and emits
/// <c>ZC_ITEM_THROW_ACK</c> back to the client. The drop carries owner
/// info so the standard 3s solo / 5s party protection windows apply.
/// </summary>
[PacketHandler(PacketHeader.CZ_ITEM_THROW)]
public class ItemThrowHandler(
    IEntityRegistry registry,
    IItemDropService drops,
    IMapWorldRegistry maps,
    IMapFlagService mapFlags,
    IStatusChangeService sc,
    ILogger<ItemThrowHandler> logger
) : IPacketHandler<MapSessionData, CZ_ITEM_THROW>
{
    public Task HandleAsync(MapSessionData session, CZ_ITEM_THROW packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }
        // rAthena pc_dropitem: pc_cant_act blocks drop (pc.cpp:6043).
        if (!player.CanAct(sc)) return Task.CompletedTask;

        var serverIndex = packet.ClientIndex - 2;
        if (session.Inventory is not { } inv
            || serverIndex < 0 || serverIndex >= inv.Count)
        {
            return Task.CompletedTask;
        }
        var item = inv[serverIndex];
        if (item.NameId == 0 || item.Amount == 0 || packet.Amount == 0
            || packet.Amount > item.Amount)
        {
            return Task.CompletedTask;
        }
        // rAthena pc_dropitem: equipped or trading items can't be dropped.
        if (item.Equip != 0 || item.EquipSwitch != 0) return Task.CompletedTask;
        if (session.Trade is not null) return Task.CompletedTask;

        // Resolve the map name from MapId so we can spawn the floor item.
        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == player.MapId);
        if (map == null)
        {
            logger.LogWarning(
                "Drop failed: char {Char} on unknown map id {MapId}",
                player.CharacterId, player.MapId);
            return Task.CompletedTask;
        }
        // rAthena pc.cpp:6043 — `nodrop` mapflag refuses drops outright.
        if (mapFlags.IsSet(map.Name, MapFlag.NoDrop))
        {
            return Task.CompletedTask;
        }

        // Owner-protect the drop so an enemy player can't snipe it in
        // the 3s window. Party id propagation would need PartyState
        // wiring; pass 0 until party-membership is loaded on session.
        drops.DropOnFloor(
            mapId: player.MapId,
            x: player.X, y: player.Y,
            itemId: (int)item.NameId,
            amount: (short)packet.Amount,
            identified: item.Identified,
            ownerCharId: player.CharacterId);

        // Decrement the inventory stack — track the row for deletion in
        // the next inventory-save IPC pass if it empties out.
        item.Amount -= packet.Amount;
        if (item.Amount == 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.RemoveAt(serverIndex);
        }

        session.EnqueuePacket(new ZC_ITEM_THROW_ACK
        {
            Index = packet.ClientIndex,
            Count = packet.Amount,
        });
        return Task.CompletedTask;
    }
}
