using Map.Server.Entities;

namespace Map.Server.Mail;

/// <summary>
/// Map-side mail entry points. Canonical for rAthena
/// <c>mail.cpp</c> (535 lines, 10 functions). Persistence + cross-
/// server delivery flow through the existing char-server mail RPCs;
/// the map-side helpers here build the outgoing message + validate
/// attachments + clear the per-session draft.
/// </summary>
public interface IMailService
{
    /// <summary>rAthena <c>mail_openmail</c>.</summary>
    int OpenMail(PlayerEntity pc);

    /// <summary>rAthena <c>mail_clear</c> — wipe the working draft.</summary>
    void Clear(PlayerEntity pc);

    /// <summary>rAthena <c>mail_send</c> — submit the draft to char-server. Resolves the receiver,
    /// debits the attached items + zeny + fee from the sender, dispatches the mail (with the
    /// attachment), and clears the draft. Returns false (no debit/clear) on any gate failure.</summary>
    Task<bool> SendAsync(PlayerEntity pc, string recipientName, string title, string body,
        CancellationToken ct = default);

    /// <summary>rAthena <c>mail_setattachment</c> — attach an item to the draft.</summary>
    bool SetAttachment(PlayerEntity pc, int inventoryIndex, int amount);

    /// <summary>rAthena <c>mail_removeitem</c> — remove an attachment.</summary>
    bool RemoveItem(PlayerEntity pc, int inventoryIndex);

    /// <summary>rAthena <c>mail_removezeny</c> — decrement attached zeny.</summary>
    bool RemoveZeny(PlayerEntity pc, long amount);

    /// <summary>rAthena <c>mail_invalid_operation</c> — quick validation gate.</summary>
    bool InvalidOperation(PlayerEntity pc);

    /// <summary>rAthena <c>mail_deliveryfail</c> — handle a refused delivery.</summary>
    void DeliveryFail(PlayerEntity pc);

    /// <summary>rAthena <c>mail_getattachment</c> — claim a mail's attachments (items + zeny) to the
    /// PC's inventory. Rejects (returns false) when the inventory is full OR the player would be
    /// over the weight limit (rAthena MAIL_ATTACH_WEIGHT / MAIL_ATTACH_ITEMCNT).</summary>
    Task<bool> GetAttachmentAsync(PlayerEntity pc, int mailId, CancellationToken ct = default);

    /// <summary>rAthena <c>mail_refresh_remaining_amount</c>.</summary>
    void RefreshRemainingAmount(PlayerEntity pc);

    /// <summary>rAthena <c>clif_parse_Mail_refreshinbox</c> → <c>intif_Mail_requestinbox</c> — fetch
    /// the PC's mailbox so the client list can render. Returns the inbox (empty list on failure).</summary>
    Task<IReadOnlyList<Core.Server.IPC.MailMessageData>> RequestInboxAsync(PlayerEntity pc, CancellationToken ct = default);

    /// <summary>rAthena <c>clif_parse_Mail_read</c> → <c>intif_Mail_read</c> — open one mail (marks it
    /// read char-side) and return its full body + attachments for the read window. Null if not found.</summary>
    Task<Core.Server.IPC.MailMessageData?> ReadMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default);

    /// <summary>rAthena <c>clif_parse_Mail_delete</c> → <c>intif_Mail_delete</c> — delete one mail.
    /// Rejects (false) if the mail still has unclaimed attachments (rAthena gate).</summary>
    Task<bool> DeleteMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default);
}
