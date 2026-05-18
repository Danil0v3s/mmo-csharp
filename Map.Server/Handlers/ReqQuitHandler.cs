using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Persistence;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client-initiated quit (ALT+E). rAthena <c>clif_parse_QuitGame</c>.
///
/// Protocol: save the player, ack with <see cref="ZC_DISCONNECT_ACK"/>
/// (<c>result = 0</c>, "OK to quit"). DO NOT proactively close the TCP —
/// the client closes it on its end after rendering the quit confirmation.
/// Closing server-side first races the ack flush and the client never sees
/// the ack (same bug we hit on the char-select-back flow).
///
/// rAthena does optionally close TCP itself when <c>drop_connection_on_quit</c>
/// is set, after explicitly flushing. Our session's flush runs on the next
/// tick, so the safer pattern is to wait for the client. The lifecycle
/// sweep picks up the dead session whenever the client closes.
///
/// <see cref="ZC_DISCONNECT_ACK"/>'s <c>result = 1</c> path (combat lockout)
/// isn't wired yet — there's no canlog_tick / battle_config equivalent for
/// it to gate on. When PvP / combat-state lands, that gate goes here.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_QUIT)]
public class ReqQuitHandler(
    IPlayerStateService playerState,
    ILogger<ReqQuitHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_QUIT>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_QUIT packet)
    {
        try
        {
            await playerState.SaveAsync(session, finalSave: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "PlayerState save on quit failed for char {CharId}",
                session.CharacterId);
        }

        session.EnqueuePacket(new ZC_DISCONNECT_ACK { Result = 0 });
    }
}
