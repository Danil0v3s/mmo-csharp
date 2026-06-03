using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX claim attached zeny. rAthena <c>clif_parse_Mail_getattach</c> (clif.cpp:16520, zeny
/// branch) → <c>intif_Mail_getattach</c>. Drives <see cref="IMailService.GetAttachmentAsync"/> and
/// emits <c>ZC_ACK_ZENY_FROM_MAIL</c>.
///
/// NOTE (GP-MAIL): the map-side claim is currently combined (zeny + items together) because the
/// char-side <c>MailGetAttachment</c> RPC settles the whole attachment at once. The
/// fully-separated partial zeny-only / item-only claims need a char-side partial-claim path — see
/// the GP-MAIL ticket Progress log (this vertical's remaining packet/IPC work).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_ZENY_FROM_MAIL)]
public class MailGetZenyHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailGetZenyHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_ZENY_FROM_MAIL>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_ZENY_FROM_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        var ok = await mail.GetAttachmentAsync(player, (int)packet.MailId);
        logger.LogDebug("mail_getattach(zeny): char {Char} mail #{Mail} = {Ok}", player.CharacterId, packet.MailId, ok);
        session.EnqueuePacket(new ZC_ACK_ZENY_FROM_MAIL
        {
            MailId = packet.MailId,
            OpenType = packet.OpenType,
            Result = ok ? ZC_ACK_ZENY_FROM_MAIL.Success : ZC_ACK_ZENY_FROM_MAIL.Error,
        });
    }
}
