using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Chat;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Chat;

/// <summary>
/// Guild chat line. rAthena <c>clif_parse_GuildMessage</c>
/// (clif.cpp:14565) → <c>guild_send_message</c>. Forwards to
/// <see cref="IChatService.SendGuildAsync"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_GUILD_CHAT)]
public class GuildChatHandler(
    IEntityRegistry registry,
    IChatService chat,
    ILogger<GuildChatHandler> logger
) : IPacketHandler<MapSessionData, CZ_GUILD_CHAT>
{
    public async Task HandleAsync(MapSessionData session, CZ_GUILD_CHAT packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity sender)
        {
            return;
        }
        if (sender.GuildId == 0) return;

        try
        {
            await chat.SendGuildAsync(sender, packet.Text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Guild chat failed: char {Char} guild {Guild}",
                sender.CharacterId, sender.GuildId);
        }
    }
}
