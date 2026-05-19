using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Chat;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Chat;

/// <summary>
/// Whisper from one player to a named target. rAthena
/// <c>clif_parse_WisMessage</c> (clif.cpp:11835). Drives
/// <see cref="IChatService.SendWhisperAsync"/> — same-map delivery is
/// immediate, cross-map hands off to char via the existing
/// <c>InterWhisper</c> IPC.
/// </summary>
[PacketHandler(PacketHeader.CZ_WHISPER)]
public class WhisperHandler(
    IEntityRegistry registry,
    IChatService chat,
    ILogger<WhisperHandler> logger
) : IPacketHandler<MapSessionData, CZ_WHISPER>
{
    public async Task HandleAsync(MapSessionData session, CZ_WHISPER packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity sender)
        {
            return;
        }

        try
        {
            await chat.SendWhisperAsync(sender, session, packet.TargetName, packet.Text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Whisper failed: char {Char} → {Target}",
                sender.CharacterId, packet.TargetName);
        }
    }
}
