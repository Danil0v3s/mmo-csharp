using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Actions;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// CZ_REQUEST_ACTION dispatcher. rAthena <c>clif_parse_ActionRequest_sub</c>
/// (clif.cpp:11671). The action byte is the discriminator; each
/// action code resolves to its own <see cref="IActionHandler"/>
/// strategy (single-attack, continuous-attack, sit, stand, pickup,
/// etc.). Adding a new action ships a new handler class + DI line —
/// no switch case in this handler to edit.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_ACTION)]
public class RequestActionHandler(
    IEntityRegistry registry,
    ActionRegistry actions,
    ILogger<RequestActionHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQUEST_ACTION>
{
    public Task HandleAsync(MapSessionData session, CZ_REQUEST_ACTION packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var handler = actions.Get(packet.Action);
        if (handler == null)
        {
            logger.LogDebug(
                "Unhandled action code {Action} from char {Char}",
                packet.Action, player.CharacterId);
            return Task.CompletedTask;
        }
        handler.Apply(player, packet.TargetId);
        return Task.CompletedTask;
    }
}
