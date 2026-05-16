using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Session;
using Map.Server.Visibility;

namespace Map.Server.Handlers;

/// <summary>
/// Client walk request. rAthena <c>clif_parse_WalkToXY</c>: feeds the cell
/// into the movement service, and on accept echoes <c>ZC_NOTIFY_PLAYERMOVE</c>
/// back to the walker and <c>ZC_NOTIFY_MOVE</c> to AOI peers so they
/// interpolate the walk client-side. Per-step server-side advancement is
/// handled by <see cref="MovementService"/>'s scheduler callbacks.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_MOVE)]
public class RequestMoveHandler(
    IEntityRegistry registry,
    IMovementService movement,
    IVisibilityService visibility,
    ILogger<RequestMoveHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQUEST_MOVE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQUEST_MOVE packet)
    {
        if (session.AuthState != MapAuthState.Spawned || session.EntityId is not { } eid)
        {
            return Task.CompletedTask;
        }

        var entity = registry.Get(eid);
        if (entity == null) return Task.CompletedTask;

        var fromX = entity.X;
        var fromY = entity.Y;

        if (!movement.TryStartWalk(entity, packet.TargetX, packet.TargetY))
        {
            logger.LogDebug(
                "Walk rejected for entity {Id}: ({FromX},{FromY}) -> ({ToX},{ToY}) unreachable",
                eid, fromX, fromY, packet.TargetX, packet.TargetY);
            return Task.CompletedTask;
        }

        var startTime = (uint)Environment.TickCount;
        var targetX = entity.Walk?.TargetX ?? packet.TargetX;
        var targetY = entity.Walk?.TargetY ?? packet.TargetY;

        session.EnqueuePacket(new ZC_NOTIFY_PLAYERMOVE
        {
            StartTime = startTime,
            FromX = fromX, FromY = fromY,
            ToX = targetX, ToY = targetY,
        });

        visibility.NotifyMoveToArea(entity, fromX, fromY, targetX, targetY, startTime);
        return Task.CompletedTask;
    }
}
