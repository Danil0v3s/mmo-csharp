using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// "Remember this warp point" — rAthena <c>clif_parse_RequestMemo</c>
/// (<c>CZ_REMEMBER_WARPPOINT</c> 0x011d → <c>pc_memo(sd, -1)</c>). The player pressed the
/// Warp Portal memo button; route it to <see cref="IPlayerPositionHelpers.Memo"/> with
/// <c>slot = -1</c> (insert-at-0). The helper enforces the AL_WARP level gate, the
/// <c>NoMemo</c>/<c>NoWarpTo</c> mapflags, and the list shift.
/// </summary>
[PacketHandler(PacketHeader.CZ_REMEMBER_WARPPOINT)]
public class RememberWarpPointHandler(
    IEntityRegistry registry,
    IPlayerPositionHelpers positions,
    ILogger<RememberWarpPointHandler> logger
) : IPacketHandler<MapSessionData, CZ_REMEMBER_WARPPOINT>
{
    public Task HandleAsync(MapSessionData session, CZ_REMEMBER_WARPPOINT packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        if (!positions.Memo(player, -1))
            logger.LogDebug("pc_memo refused: char {Char}", player.CharacterId);
        return Task.CompletedTask;
    }
}
