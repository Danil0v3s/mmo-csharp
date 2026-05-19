using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Session;

namespace Map.Server.Handlers.Equip;

/// <summary>
/// Player asks to take off an equipped item. rAthena
/// <c>clif_parse_UnequipItem</c> (clif.cpp:12140). Drives
/// <see cref="IEquipService.Unequip"/>, then emits
/// <c>ZC_REQ_TAKEOFF_EQUIP_ACK</c> with the slot bitmask the item used
/// to wear and a 1/0 success flag.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_TAKEOFF_EQUIP)]
public class UnequipItemHandler(
    IEntityRegistry registry,
    IEquipService equip,
    ILogger<UnequipItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_TAKEOFF_EQUIP>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_TAKEOFF_EQUIP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var serverIndex = packet.ClientIndex - 2;
        var result = equip.Unequip(session, serverIndex, out var clearedPos);
        if (result != EquipOpResult.Ok)
        {
            logger.LogDebug(
                "Unequip rejected: char {Char} slot {Slot}: {Reason}",
                player.CharacterId, serverIndex, result);
        }

        session.EnqueuePacket(EquipNotifier.BuildTakeOffAck(
            packet.ClientIndex, clearedPos, success: result == EquipOpResult.Ok));
        return Task.CompletedTask;
    }
}
