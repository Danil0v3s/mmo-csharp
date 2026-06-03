using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX send. rAthena <c>clif_parse_Mail_send</c> (clif.cpp:16784) → <c>mail_send</c>. The composed
/// zeny rides this packet (rAthena stages it via <c>mail_setitem(0, zeny)</c>); the item attachments
/// were already staged by the add-item packets. Pushes the draft zeny, then drives
/// <see cref="IMailService.SendAsync"/> (validates + debits + dispatches) and emits
/// <c>ZC_WRITE_MAIL_RESULT</c>.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_SEND_MAIL)]
public class MailSendHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailSendHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_SEND_MAIL>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_SEND_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        // The send packet carries the attached zeny (the items were staged during composition).
        player.MailDraftZeny = Math.Max(0, packet.Zeny);
        var ok = await mail.SendAsync(player, packet.Receiver, packet.Title, packet.Body);
        logger.LogDebug("mail_send: char {Char} → {To} = {Ok}", player.CharacterId, packet.Receiver, ok);
        session.EnqueuePacket(new ZC_WRITE_MAIL_RESULT
        {
            Result = ok ? ZC_WRITE_MAIL_RESULT.Success : ZC_WRITE_MAIL_RESULT.Failed,
        });
    }
}
