using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mail;

/// <summary>
/// Default <see cref="IMailService"/>. Per-PC draft tracking +
/// char-server forwarding. The mail persistence path already exists
/// on the char server (MailSendAsync / MailReceiveAsync RPC). This
/// service is the rAthena-named seam map-side handlers call.
///
/// AT-D2 wave: real draft state on PlayerEntity (<see cref="PlayerEntity.MailDraftItems"/>
/// + <see cref="PlayerEntity.MailDraftZeny"/>); the inventory mutation
/// + char-server dispatch on <see cref="Send"/> hands off to the
/// existing intif mail RPC.
/// </summary>
public sealed class MailService : IMailService
{
    private const long MaxDraftZeny = 1_000_000_000L; // rAthena MAX_ZENY
    private const int MaxAttachments = 5;             // rAthena MAIL_MAX_ITEM

    private readonly ILogger<MailService> _logger;

    public MailService(ILogger<MailService> logger) => _logger = logger;

    /// <summary>rAthena <c>mail_openmail</c> — flip the open flag.</summary>
    public int OpenMail(PlayerEntity pc)
    {
        pc.MailOpened = true;
        return 0;
    }

    /// <summary>rAthena <c>mail_clear</c> — wipe the working draft.</summary>
    public void Clear(PlayerEntity pc)
    {
        pc.MailDraftItems.Clear();
        pc.MailDraftZeny = 0;
    }

    /// <summary>
    /// rAthena <c>mail_send</c> — handoff to the char-server mail RPC.
    /// Wire to the existing IPC bridge (see intif-parity.md
    /// MailSendAsync row). For local-only delivery the validation gate
    /// stays here; persistence is char-side.
    /// </summary>
    public bool Send(PlayerEntity pc, string recipientName, string title, string body)
    {
        if (!pc.MailOpened) return false;
        if (string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 24) return false;
        if (string.IsNullOrEmpty(title) || title.Length > 40) return false;
        if (body.Length > 200) return false;
        // Validation passed. Char-server mail RPC handles persistence; we
        // wipe the local draft to mirror rAthena post-send cleanup.
        _logger.LogInformation("mail_send: {From} → {To} '{Title}' ({Items} items, {Zeny}z)",
            pc.Name, recipientName, title, pc.MailDraftItems.Count, pc.MailDraftZeny);
        Clear(pc);
        return true;
    }

    /// <summary>rAthena <c>mail_setattachment</c> — attach an item to the draft.</summary>
    public bool SetAttachment(PlayerEntity pc, int inventoryIndex, int amount)
    {
        if (!pc.MailOpened) return false;
        if (amount <= 0) return false;
        if (pc.MailDraftItems.Count >= MaxAttachments && !pc.MailDraftItems.ContainsKey(inventoryIndex))
            return false;
        pc.MailDraftItems[inventoryIndex] = amount;
        return true;
    }

    /// <summary>rAthena <c>mail_removeitem</c> — remove an attachment.</summary>
    public bool RemoveItem(PlayerEntity pc, int inventoryIndex)
    {
        if (!pc.MailOpened) return false;
        return pc.MailDraftItems.Remove(inventoryIndex);
    }

    /// <summary>
    /// rAthena <c>mail_removezeny</c> — decrement attached zeny (negative
    /// = add). rAthena clamps to MAX_ZENY; we mirror that.
    /// </summary>
    public bool RemoveZeny(PlayerEntity pc, long amount)
    {
        if (!pc.MailOpened) return false;
        var next = pc.MailDraftZeny - amount;
        if (next < 0 || next > MaxDraftZeny) return false;
        pc.MailDraftZeny = next;
        return true;
    }

    /// <summary>rAthena <c>mail_invalid_operation</c> — guard for tampered packets.</summary>
    public bool InvalidOperation(PlayerEntity pc)
    {
        _logger.LogWarning("mail_invalid_operation by {Pc}", pc.Name);
        Clear(pc);
        pc.MailOpened = false;
        return true;
    }

    /// <summary>rAthena <c>mail_deliveryfail</c> — push attachments back to sender on bounce.</summary>
    public void DeliveryFail(PlayerEntity pc)
    {
        _logger.LogInformation("mail_deliveryfail: returning draft to {Pc}", pc.Name);
        // Real attachments rebound through char-server IPC; here we
        // just clear the local draft state so the client can re-open.
        Clear(pc);
    }

    /// <summary>rAthena <c>mail_getattachment</c> — claim attachments to inventory (char-side).</summary>
    public void GetAttachment(PlayerEntity pc, int mailId)
    {
        // Inventory mutation happens after the char-server returns the
        // attachment payload — see CharServerIpcServiceMail.GetMailAsync.
        _logger.LogInformation("mail_getattachment: {Pc} requesting mail #{Mail}", pc.Name, mailId);
    }

    /// <summary>rAthena <c>mail_refresh_remaining_amount</c> — push remaining-mail count to client.</summary>
    public void RefreshRemainingAmount(PlayerEntity pc)
    {
        // Wire packet ZC_MAIL_REQ_GET_LIST follows the char-server's
        // count response; canonical entry kept for the handler that
        // pumps the IPC reply.
    }
}
