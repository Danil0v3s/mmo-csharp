using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX unstage an item. rAthena <c>clif_parse_Mail_winopen</c> (clif.cpp:16763, remove-item branch)
/// → <c>mail_removeitem</c> → <c>clif_mail_removeitem</c>. Drives <see cref="IMailService.RemoveItem"/>
/// and acks (<c>ZC_ACK_REMOVE_ITEM_MAIL</c>) with the new running mail weight.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_REMOVE_ITEM_MAIL)]
public class MailRemoveItemHandler(
    IEntityRegistry registry,
    IMailService mail,
    IItemCatalog? items = null
) : IPacketHandler<MapSessionData, CZ_REQ_REMOVE_ITEM_MAIL>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_REMOVE_ITEM_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var serverIndex = packet.ClientIndex - 2;
        var ok = mail.RemoveItem(player, serverIndex);
        session.EnqueuePacket(new ZC_ACK_REMOVE_ITEM_MAIL
        {
            Success = ok,
            Index = (short)packet.ClientIndex,
            Count = (ushort)Math.Max(0, (int)packet.Amount),
            Weight = (short)Math.Min(short.MaxValue, MailAddItemHandler.DraftWeight(player, session, items)),
        });
        return Task.CompletedTask;
    }
}
