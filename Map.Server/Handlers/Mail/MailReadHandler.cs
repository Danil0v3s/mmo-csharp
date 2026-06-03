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
/// RODEX read one mail. rAthena <c>clif_parse_Mail_read</c> (clif.cpp:16422) → <c>intif_Mail_read</c>
/// (marks read char-side) → <c>clif_Mail_read</c>. Drives <see cref="IMailService.ReadMailAsync"/>
/// and emits <c>ZC_ACK_READ_RODEX</c> (body text + zeny + the attached items).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_READ_MAIL)]
public class MailReadHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailReadHandler> logger,
    IItemCatalog? items = null
) : IPacketHandler<MapSessionData, CZ_REQ_READ_MAIL>
{
    public async Task HandleAsync(MapSessionData session, CZ_REQ_READ_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        var msg = await mail.ReadMailAsync(player, packet.MailId);
        if (msg == null)
        {
            logger.LogDebug("mail_read: char {Char} mail #{Mail} not found", player.CharacterId, packet.MailId);
            return;
        }
        session.EnqueuePacket(BuildRead(packet.OpenType, msg, items));
    }

    /// <summary>Project a persisted mail into the read-window packet. Item display hints
    /// (<c>type</c>) are resolved from the item catalog; <c>viewSprite</c>/<c>location</c> default to 0
    /// (the client renders the item from its id) — see the GP-MAIL Progress log.</summary>
    internal static ZC_ACK_READ_RODEX BuildRead(byte openType, Core.Server.IPC.MailMessageData msg, IItemCatalog? catalog)
    {
        var items = new List<MailReadItem>(msg.Items.Count);
        foreach (var a in msg.Items)
        {
            if (a.NameId == 0 || a.Amount == 0) continue;
            var type = catalog?.Get(a.NameId) is { } def ? ItemTypeCodes.FromDbString(def.Type) : (byte)0;
            items.Add(new MailReadItem
            {
                Count = (short)a.Amount,
                ItemId = a.NameId,
                Identified = a.Identify != 0,
                Damaged = a.Attribute != 0,
                Refine = (sbyte)a.Refine,
                Card0 = a.Card0, Card1 = a.Card1, Card2 = a.Card2, Card3 = a.Card3,
                Type = type,
                BindOnEquip = (ushort)(a.Bound != 0 ? 2 : 0),
                Options = new[]
                {
                    ((short)a.OptionId0, (short)a.OptionVal0, (sbyte)a.OptionParm0),
                    ((short)a.OptionId1, (short)a.OptionVal1, (sbyte)a.OptionParm1),
                    ((short)a.OptionId2, (short)a.OptionVal2, (sbyte)a.OptionParm2),
                    ((short)a.OptionId3, (short)a.OptionVal3, (sbyte)a.OptionParm3),
                    ((short)a.OptionId4, (short)a.OptionVal4, (sbyte)a.OptionParm4),
                },
            });
        }
        return new ZC_ACK_READ_RODEX
        {
            OpenType = openType,
            MailId = msg.MailId,
            Zeny = msg.Zeny,
            Body = msg.Body,
            Items = items,
        };
    }
}
