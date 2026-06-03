using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX stage an item onto the draft. rAthena <c>clif_parse_Mail_setattach</c> (clif.cpp:16702) →
/// <c>mail_setitem</c> → <c>clif_Mail_setattachment</c>. Drives <see cref="IMailService.SetAttachment"/>
/// and acks the staged item (<c>ZC_ACK_ADD_ITEM_RODEX</c>) with its details + the running mail weight.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_ADD_ITEM_TO_MAIL)]
public class MailAddItemHandler(
    IEntityRegistry registry,
    IMailService mail,
    IItemCatalog? items = null
) : IPacketHandler<MapSessionData, CZ_REQ_ADD_ITEM_TO_MAIL>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_ADD_ITEM_TO_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var serverIndex = packet.ClientIndex - 2;
        var item = session.Inventory?.FirstOrDefault(i => i.ServerIndex == serverIndex);
        var ok = item != null && mail.SetAttachment(player, serverIndex, packet.Count);

        if (!ok || item == null)
        {
            session.EnqueuePacket(new ZC_ACK_ADD_ITEM_RODEX { Result = 1, Index = (short)packet.ClientIndex });
            return Task.CompletedTask;
        }

        session.EnqueuePacket(new ZC_ACK_ADD_ITEM_RODEX
        {
            Result = 0,
            Index = (short)packet.ClientIndex,
            Count = packet.Count,
            ItemId = item.NameId,
            Type = items?.Get(item.NameId) is { } def ? ItemTypeCodes.FromDbString(def.Type) : (byte)0,
            Identified = item.Identified,
            Damaged = item.Attribute != 0,
            Card0 = item.Card0, Card1 = item.Card1, Card2 = item.Card2, Card3 = item.Card3,
            Options = new[]
            {
                (item.Options[0].Id, item.Options[0].Value, item.Options[0].Param),
                (item.Options[1].Id, item.Options[1].Value, item.Options[1].Param),
                (item.Options[2].Id, item.Options[2].Value, item.Options[2].Param),
                (item.Options[3].Id, item.Options[3].Value, item.Options[3].Param),
                (item.Options[4].Id, item.Options[4].Value, item.Options[4].Param),
            },
            Weight = (short)Math.Min(short.MaxValue, DraftWeight(player, session, items)),
            Refine = (sbyte)item.Refine,
            Grade = (sbyte)item.EnchantGrade,
        });
        return Task.CompletedTask;
    }

    /// <summary>The running weight of every item currently staged on the mail draft.</summary>
    internal static long DraftWeight(PlayerEntity pc, MapSessionData session, IItemCatalog? items)
    {
        if (items == null || session.Inventory == null) return 0;
        long total = 0;
        foreach (var (serverIndex, amount) in pc.MailDraftItems)
        {
            var inv = session.Inventory.FirstOrDefault(i => i.ServerIndex == serverIndex);
            if (inv != null) total += (long)(items.Get(inv.NameId)?.Weight ?? 0) * amount;
        }
        return total;
    }
}
