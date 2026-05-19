using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Gm;
using Map.Server.Session;
using Map.Server.Visibility;

namespace Map.Server.Handlers;

/// <summary>
/// Global chat / GM command entry point. rAthena
/// <c>clif_parse_GlobalMessage</c> equivalent. For MS3 first slice we route
/// <c>@</c>-prefix messages to the GM command framework and silently drop
/// everything else — the public chat broadcast (rAthena <c>clif_message</c>)
/// lands when <see href="../../.agents/migrations/map/adjacent/chat.md"/>
/// arrives.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_CHAT)]
public class ChatMessageHandler(
    IEntityRegistry registry,
    IGmCommandRegistry commands,
    IVisibilityService visibility,
    ILogger<ChatMessageHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQUEST_CHAT>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQUEST_CHAT packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        if (!GmCommandParser.TryParse(packet.Text, out var name, out var args))
        {
            // Plain chat — rAthena clif_message: broadcast ZC_NOTIFY_CHAT
            // to the speaker's AOI. The text the client sends is already
            // formatted as "<name>: <utterance>" by the client itself.
            visibility.SendToArea(player, new ZC_NOTIFY_CHAT
            {
                SourceId = player.Id.Value,
                Message = packet.Text,
            });
            return;
        }

        var command = commands.Get(name);
        if (command == null)
        {
            visibility.SendToSelf(player, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@{name}: unknown command.",
            });
            return;
        }

        if (session.GroupId < (uint)command.MinGroupId)
        {
            logger.LogWarning(
                "Char {CharId} (group {Group}) attempted GM command @{Name} (requires group {Required})",
                player.CharacterId, session.GroupId, command.Name, command.MinGroupId);
            visibility.SendToSelf(player, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@{name}: permission denied.",
            });
            return;
        }

        try
        {
            await command.ExecuteAsync(player, args, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GM command @{Name} threw for char {CharId}",
                command.Name, player.CharacterId);
            visibility.SendToSelf(player, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@{name}: error — see server log.",
            });
        }
    }
}
