using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// CZ_REQUEST_ACTION dispatcher. rAthena <c>clif_parse_ActionRequest_sub</c>
/// (clif.cpp:11671). Handles attack (single + continuous), sit / stand
/// here on the same packet — the action byte is the discriminator.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_ACTION)]
public class RequestActionHandler(
    IEntityRegistry registry,
    IAttackService attackService,
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

        // Action codes from rAthena (clif.cpp:11784):
        //   0 = single attack  (DMG_NORMAL)
        //   1 = pick up item
        //   2 = sit down       (DMG_SIT_DOWN)
        //   3 = stand up       (DMG_STAND_UP)
        //   7 = continuous attack (DMG_REPEAT)
        switch (packet.Action)
        {
            case 0:
            case 7:
                {
                    var continuous = packet.Action == 7;
                    if (!attackService.StartAttack(player, new EntityId(packet.TargetId), continuous))
                    {
                        logger.LogDebug(
                            "Attack rejected: char {Char} → target {Target}",
                            player.CharacterId, packet.TargetId);
                    }
                    break;
                }

            case 2:
            case 3:
                // Sit/stand visual ack is a future slice — pc_setsit /
                // pc_setstand path needs to broadcast ZC_SITTING. Cancel
                // any attack first to match rAthena.
                attackService.StopAttack(player);
                break;

            case 1:
                // Pickup is already routed via CZ_ITEM_PICKUP; this case
                // is the client's alternate path. Nothing to do here.
                break;

            default:
                logger.LogDebug(
                    "Unhandled action code {Action} from char {Char}",
                    packet.Action, player.CharacterId);
                break;
        }
        return Task.CompletedTask;
    }
}
