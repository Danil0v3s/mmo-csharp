using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX open mailbox / request inbox. rAthena <c>clif_parse_Mail_refreshinbox</c> (clif.cpp:16240)
/// → <c>intif_Mail_requestinbox</c> → <c>clif_Mail_refreshinbox</c>. Flips the mailbox-open flag,
/// fetches the inbox via <see cref="IMailService.RequestInboxAsync"/>, and emits
/// <c>ZC_ACK_MAIL_LIST</c> so the client populates the mailbox window.
/// </summary>
[PacketHandler(PacketHeader.CZ_OPEN_MAILBOX)]
public class MailOpenHandler(
    IEntityRegistry registry,
    IMailService mail,
    ILogger<MailOpenHandler> logger
) : IPacketHandler<MapSessionData, CZ_OPEN_MAILBOX>
{
    public async Task HandleAsync(MapSessionData session, CZ_OPEN_MAILBOX packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return;
        }

        mail.OpenMail(player);
        var inbox = await mail.RequestInboxAsync(player);
        session.EnqueuePacket(BuildList(inbox));
        logger.LogDebug("mail open: char {Char} sent {N} mail(s)", player.CharacterId, inbox.Count);
    }

    /// <summary>Map the persisted inbox rows to the RODEX list packet (rAthena fakes a 1-year
    /// scheduled-deletion when none is set, since this port doesn't track auto-deletion).</summary>
    internal static ZC_ACK_MAIL_LIST BuildList(IReadOnlyList<Core.Server.IPC.MailMessageData> inbox)
    {
        const uint oneYear = 365u * 24 * 60 * 60;
        var rows = new List<MailListEntry>(inbox.Count);
        foreach (var m in inbox)
        {
            rows.Add(new MailListEntry
            {
                Type = 0, // MAIL_INBOX_NORMAL — the persistence layer doesn't split tabs yet
                MailId = m.MailId,
                Read = m.Opened,
                HasZeny = m.Zeny > 0,
                HasItems = m.Items.Count > 0,
                IsNpc = m.SenderAccountId == 0,
                SenderName = m.SenderName,
                DeletionSeconds = oneYear,
                Title = m.Title,
            });
        }
        return new ZC_ACK_MAIL_LIST { Mails = rows };
    }
}
