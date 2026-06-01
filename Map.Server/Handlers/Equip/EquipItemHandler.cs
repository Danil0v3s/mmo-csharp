using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;

namespace Map.Server.Handlers.Equip;

/// <summary>
/// Player asks to wear an inventory item. rAthena
/// <c>clif_parse_EquipItem</c> (clif.cpp:12083). Drives
/// <see cref="IEquipService.Equip"/>, then emits
/// <c>ZC_REQ_WEAR_EQUIP_ACK</c> with the actual slot bitmask + view id.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_WEAR_EQUIP)]
public class EquipItemHandler(
    IEntityRegistry registry,
    IEquipService equip,
    IItemCatalog catalog,
    ILogger<EquipItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_WEAR_EQUIP>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_WEAR_EQUIP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        // rAthena pc_equipitem: a channelled skill (e.g. WM_SEVERE_RAINSTORM)
        // can lock equipment swaps for its duration via sd->canequip_tick.
        if (player.CanEquipTick > Environment.TickCount64)
        {
            session.EnqueuePacket(EquipNotifier.BuildWearAck(
                packet.ClientIndex, 0, 0, EquipOpResult.Fail));
            return Task.CompletedTask;
        }

        // server_index = client_index − 2 (rAthena server_index()).
        var serverIndex = packet.ClientIndex - 2;
        var result = equip.Equip(session, serverIndex, packet.Position, out var appliedPos);
        ushort viewId = 0;
        if (result == EquipOpResult.Ok && session.Inventory is { } inv
            && serverIndex >= 0 && serverIndex < inv.Count)
        {
            viewId = EquipNotifier.ResolveViewId(catalog, inv[serverIndex].NameId);
        }
        else
        {
            logger.LogDebug(
                "Equip rejected: char {Char} slot {Slot} pos {Pos:X}: {Reason}",
                player.CharacterId, serverIndex, packet.Position, result);
        }

        session.EnqueuePacket(EquipNotifier.BuildWearAck(
            packet.ClientIndex, appliedPos, viewId, result));
        return Task.CompletedTask;
    }
}
