using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX claim item attachments. rAthena <c>clif_parse_Mail_getattach</c> (clif.cpp:16520, item
/// branch) → <c>intif_Mail_getattach</c>. Drives <see cref="IMailService.GetAttachmentAsync"/>
/// (which credits the items + zeny to the player, enforcing the inventory-full + overweight gates)
/// and emits <c>ZC_ACK_ITEM_FROM_MAIL</c> with the success / error result.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_ITEM_FROM_MAIL)]
public class MailGetItemHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailGetItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_ITEM_FROM_MAIL>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_ITEM_FROM_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        var ok = await mail.GetAttachmentAsync(player, (int)packet.MailId);
        logger.LogDebug("mail_getattach(item): char {Char} mail #{Mail} = {Ok}", player.CharacterId, packet.MailId, ok);
        session.EnqueuePacket(new ZC_ACK_ITEM_FROM_MAIL
        {
            MailId = packet.MailId,
            OpenType = packet.OpenType,
            Result = ok ? ZC_ACK_ITEM_FROM_MAIL.Success : ZC_ACK_ITEM_FROM_MAIL.Error,
        });
    }
}
