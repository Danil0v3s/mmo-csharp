using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Persistence;
using Map.Server.Services;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// "Back to char-select" / "Respawn after death". rAthena <c>clif_parse_Restart</c>.
///
/// <list type="bullet">
///   <item><c>type = 0</c> — respawn. Heal + warp to save point. Pending the
///     <c>pc_setpos</c> port + savepoint state; for now we log and no-op.</item>
///   <item><c>type = 1</c> — return to char-select. Save state, clear the
///     online flag on char-server (rAthena <c>chrif_charselectreq</c>
///     equivalent), then ack with <c>ZC_RESTART_ACK</c>. The client closes
///     the map TCP itself and reconnects to char-server for the character
///     list.</item>
/// </list>
///
/// Why clear-online-before-ack: if we ack-and-wait, the client races to
/// char-server while the account is still flagged online — the
/// dup-online check at char-server fires and refuses the reconnect. The
/// lifecycle sweep would clear it too, but only after the map TCP close
/// lands, which is too late for a fast client.
/// </summary>
[PacketHandler(PacketHeader.CZ_RESTART)]
public class RestartHandler(
    IPlayerStateService playerState,
    ICharServerIpcService charServerIpc,
    IPcDeathService pcDeath,
    IEntityRegistry registry,
    ILogger<RestartHandler> logger
) : IPacketHandler<MapSessionData, CZ_RESTART>
{
    public async Task HandleAsync(MapSessionData session, CZ_RESTART packet)
    {
        if (packet.Type != 1)
        {
            // Respawn — rAthena clif_parse_Restart case 0x00 → pc_respawn.
            if (session.EntityId is { } eid
                && registry.Get(eid) is PlayerEntity pc
                && pcDeath.IsDead(pc))
            {
                pcDeath.Respawn(pc);
            }
            else
            {
                logger.LogDebug(
                    "Char {CharId} sent respawn (type=0) but isn't in a dead state",
                    session.CharacterId);
            }
            return;
        }

        // Save first so the player's state is durable even if the client
        // disconnects abruptly between the ack and our cleanup sweep.
        try
        {
            await playerState.SaveAsync(session, finalSave: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "PlayerState save on char-select-return failed for char {CharId}",
                session.CharacterId);
        }

        // Clear the online flag AND pre-register the returning client on
        // char-server so the imminent CH_REQ_TO_CONNECT is accepted without
        // a second login-server auth (the original auth_node was consumed
        // by the initial char→map handoff). Together these match rAthena's
        // chrif_charselectreq + chmapif_parse_reqcharselect flow.
        if (session.AccountId is { } accountId && session.CharacterId is { } charId)
        {
            try
            {
                await charServerIpc.SetCharacterOfflineAsync(accountId, charId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "SetCharacterOffline on char-select-return failed for char {CharId}",
                    charId);
            }

            try
            {
                await charServerIpc.NotifyCharacterSelectAuthOkAsync(
                    accountId: accountId,
                    loginId1: session.LoginId1 ?? 0,
                    loginId2: 0,         // not tracked map-side; char-server ignores
                    ip: 0,                // unused by our match
                    sex: session.Sex);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "NotifyCharacterSelectAuthOk on char-select-return failed for char {CharId}",
                    charId);
            }
        }

        // Ack last. Server does NOT close the TCP — the client does that on
        // its end after processing the ack. MapSessionLifecycle's sweep
        // picks up the dead session whenever the close arrives.
        session.EnqueuePacket(new ZC_RESTART_ACK { Type = 1 });
    }
}
