using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Status;

namespace Map.Server.Handlers;

/// <summary>
/// CZ_USE_ITEM dispatcher. rAthena <c>clif_parse_UseItem</c>
/// (clif.cpp:12051). Translates the wire-side slot index (client_index)
/// to the server-side index by subtracting 2 — rAthena's
/// <c>server_index</c> helper.
/// </summary>
[PacketHandler(PacketHeader.CZ_USE_ITEM)]
public class UseItemHandler(
    IEntityRegistry registry,
    IItemUseService itemUse,
    Map.Server.Combat.IPcDeathService pcDeath,
    IStatusChangeService sc,
    ILogger<UseItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_USE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_USE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        // rAthena: pc_isdead → CLR_DEAD broadcast, return.
        if (pcDeath.IsDead(player)) return Task.CompletedTask;
        // rAthena pc_useitem: pc_cant_act refuses item use (pc.cpp:5803).
        if (!player.CanAct(sc)) return Task.CompletedTask;

        // Wire index is client_index = server_index + 2.
        var serverIndex = packet.Index - 2;
        if (!itemUse.UseItem(player, serverIndex))
        {
            logger.LogDebug(
                "Item use failed: char {Char} slot {Slot}",
                player.CharacterId, serverIndex);
        }
        return Task.CompletedTask;
    }
}
