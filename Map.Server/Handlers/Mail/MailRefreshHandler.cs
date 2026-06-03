using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX refresh inbox list. rAthena <c>clif_parse_Mail_refreshinbox</c> (the 0x0ac1 variant). At
/// PACKETVER ≥ 20170419 the server resends the whole inbox, so this is the same action as opening
/// the mailbox — re-fetch + re-emit <c>ZC_ACK_MAIL_LIST</c> via <see cref="MailOpenHandler.BuildList"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_REFRESH_MAIL_LIST)]
public class MailRefreshHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailRefreshHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_REFRESH_MAIL_LIST>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_REFRESH_MAIL_LIST packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        var inbox = await mail.RequestInboxAsync(player);
        session.EnqueuePacket(MailOpenHandler.BuildList(inbox));
        logger.LogDebug("mail refresh: char {Char} resent {N} mail(s)", player.CharacterId, inbox.Count);
    }
}
