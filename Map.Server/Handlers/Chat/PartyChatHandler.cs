using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Chat;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Chat;

/// <summary>
/// Party chat line. rAthena <c>clif_parse_PartyMessage</c>
/// (clif.cpp:13948) → <c>party_send_message</c>. The handler hands the
/// text to <see cref="IChatService.SendPartyAsync"/>, which fans the
/// packet to locally-loaded party members and routes the rest through
/// char IPC.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_CHAT_PARTY)]
public class PartyChatHandler(
    IEntityRegistry registry,
    IChatService chat,
    ILogger<PartyChatHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQUEST_CHAT_PARTY>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQUEST_CHAT_PARTY packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity sender)
        {
            return;
        }
        if (sender.PartyId == 0) return;

        try
        {
            await chat.SendPartyAsync(sender, packet.Text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Party chat failed: char {Char} party {Party}",
                sender.CharacterId, sender.PartyId);
        }
    }
}
