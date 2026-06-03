using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX delete. rAthena <c>clif_parse_Mail_delete</c> (clif.cpp:16626) →
/// <c>intif_Mail_delete</c>. Drives <see cref="IMailService.DeleteMailAsync"/> and, on
/// success, emits <c>ZC_ACK_DELETE_MAIL</c> so the client removes the row (rAthena only
/// acks on success).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_DELETE_MAIL)]
public class MailDeleteHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailDeleteHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_DELETE_MAIL>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_DELETE_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        var ok = await mail.DeleteMailAsync(player, packet.MailId);
        if (!ok)
        {
            logger.LogDebug("mail_delete refused: char {Char} mail #{Mail}", player.CharacterId, packet.MailId);
            return;
        }
        session.EnqueuePacket(new ZC_ACK_DELETE_MAIL { OpenType = packet.OpenType, MailId = packet.MailId });
    }
}
