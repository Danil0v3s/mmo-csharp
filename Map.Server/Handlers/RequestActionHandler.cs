using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Visibility;

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
    IVisibilityService visibility,
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

            case 2: // sit down (rAthena DMG_SIT_DOWN, clif_sitting)
            case 3: // stand up (rAthena DMG_STAND_UP, clif_standing)
                {
                    // Cancel any attack first to match rAthena.
                    attackService.StopAttack(player);
                    var sitting = packet.Action == 2;
                    if (player.IsSitting == sitting) break; // no-op
                    player.IsSitting = sitting;
                    // rAthena emits ZC_NOTIFY_ACT (the legacy 008a packet)
                    // with action=Sit/Stand. ZC_NOTIFY_ACT3 carries the
                    // same shape and is the renewal client variant.
                    visibility.SendToArea(player, new ZC_NOTIFY_ACT3
                    {
                        SourceId = player.Id.Value,
                        TargetId = 0,
                        ServerTick = (uint)Environment.TickCount,
                        SourceAmotion = 0,
                        TargetAmotion = 0,
                        Damage = 0,
                        IsSpDamage = 0,
                        Div = 0,
                        ActionType = sitting ? DamageActionType.Sit : DamageActionType.Stand,
                        Damage2 = 0,
                    });
                    break;
                }

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
