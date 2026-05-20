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

    /// <summary>rAthena <c>mail_send</c> — submit the draft to char-server.</summary>
    bool Send(PlayerEntity pc, string recipientName, string title, string body);

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

    /// <summary>rAthena <c>mail_getattachment</c> — pull attachments to inventory.</summary>
    void GetAttachment(PlayerEntity pc, int mailId);

    /// <summary>rAthena <c>mail_refresh_remaining_amount</c>.</summary>
    void RefreshRemainingAmount(PlayerEntity pc);
}
