using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Services;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mail;

/// <summary>
/// Default <see cref="IMailService"/>. Per-PC draft tracking + real attachment transfer through the
/// char-server mail RPCs (FEATURE-05). Persistence lives char-side (the mail + mail_attachment
/// tables); this service debits the sender on send, credits the receiver on claim, and rebounds on
/// dispatch failure. The client packets (ZC_ACK_MAIL_SEND / inbox list / get-item ack) are owned by
/// PACKET-06 — the inventory/zeny mutation happens here regardless.
/// </summary>
public sealed class MailService : IMailService
{
    private const long MaxDraftZeny = 1_000_000_000L; // rAthena MAX_ZENY
    private const int MaxAttachments = 5;             // rAthena MAIL_MAX_ITEM
    private const int MaxInventory = 100;             // rAthena MAX_INVENTORY

    // rAthena battle_config defaults: a per-item flat fee + a percentage tax on the attached zeny.
    private const long MailAttachmentPrice = 2500;    // battle_config.mail_attachment_price
    private const long MailZenyFeePercent = 0;        // battle_config.mail_zeny_fee
    private const long BaseMaxWeight = 20000;         // rAthena pc.cpp base max weight

    private readonly ISessionManagerAccessor? _sessions;
    private readonly ICharServerIpcServiceMail? _mailIpc;
    private readonly IItemCatalog? _items;
    private readonly ILogger<MailService> _logger;

    public MailService(ILogger<MailService> logger,
        ISessionManagerAccessor? sessions = null, ICharServerIpcServiceMail? mailIpc = null,
        IItemCatalog? items = null)
    {
        _logger = logger;
        _sessions = sessions;
        _mailIpc = mailIpc;
        _items = items;
    }

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

    /// <inheritdoc />
    public async Task<bool> SendAsync(PlayerEntity pc, string recipientName, string title, string body,
        CancellationToken ct = default)
    {
        // 1. Sync gates (rAthena mail_send MAIL_SEND_FAIL checks).
        if (!pc.MailOpened) return false;
        if (string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 24) return false;
        if (string.IsNullOrEmpty(title) || title.Length > 40) return false;
        if (body.Length > 200) return false;
        if (string.Equals(recipientName, pc.Name, StringComparison.OrdinalIgnoreCase)) return false; // not self

        var session = _sessions?.GetByEntityId(pc.Id);
        var inv = session?.Inventory;
        if (session?.CharacterData == null || inv == null || _mailIpc == null) return false;

        // 2. Validate the draft against the live inventory + zeny before any debit.
        var attachments = new List<MailAttachmentItem>();
        foreach (var (index, amount) in pc.MailDraftItems)
        {
            if (amount <= 0) return false;
            var item = FindBySlot(inv, index);
            if (item == null || item.Equip != 0 || item.Amount < (uint)amount) return false; // gone / equipped / short
            attachments.Add(ToAttachment(item, amount));
        }
        var fee = pc.MailDraftZeny * MailZenyFeePercent / 100 + attachments.Count * MailAttachmentPrice;
        var totalCost = pc.MailDraftZeny + fee;
        if (totalCost < 0 || (long)session.CharacterData.Zeny < totalCost) return false; // can't afford zeny + fee

        // 3. Resolve the receiver char-side (rAthena's name → char_id check).
        var receiver = await _mailIpc.MailReceiverCheckAsync(recipientName, ct);
        if (receiver is not { Success: true }) return false;

        // 4. Debit the sender (items + zeny + fee) — rAthena pc_delitem / pc_payzeny.
        foreach (var (index, amount) in pc.MailDraftItems)
            DebitSlot(session, inv, index, amount);
        session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny - totalCost);

        // 5. Dispatch with the structured attachment.
        var resp = await _mailIpc.MailSendAsync(
            senderAccountId: pc.AccountId, senderCharacterId: pc.CharacterId, senderName: pc.Name,
            receiverAccountId: receiver.AccountId, receiverCharacterId: receiver.CharacterId,
            receiverName: receiver.ReceiverName, title: title, body: body,
            zeny: pc.MailDraftZeny, attachment: Array.Empty<byte>(), items: attachments, cancellationToken: ct);

        if (resp is not { Success: true })
        {
            // rAthena mail_deliveryfail — the send was refused; rebound the debited items + zeny.
            foreach (var a in attachments) CreditItem(session, inv, a);
            session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny + totalCost);
            _logger.LogWarning("mail_send: dispatch failed {From}->{To}; rebounded {N} items + {Zeny}z",
                pc.Name, recipientName, attachments.Count, totalCost);
            return false;
        }

        Clear(pc); // draft wiped only after a successful dispatch
        // PACKET-06: ZC_ACK_MAIL_SEND(success) emit seam.
        _logger.LogInformation("mail_send: {From}->{To} '{Title}' ({N} items, {Zeny}z, fee {Fee})",
            pc.Name, recipientName, title, attachments.Count, pc.MailDraftZeny, fee);
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

    /// <summary>rAthena <c>mail_removezeny</c> — decrement attached zeny (negative = add).</summary>
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

    /// <summary>rAthena <c>mail_deliveryfail</c> — a draft abort before dispatch. Attachments are
    /// only flagged in the draft (the real items stay in inventory until <see cref="SendAsync"/>
    /// debits them), so clearing the draft returns the player to a clean state with no item loss. The
    /// post-debit dispatch-failure rebound is handled inline in <see cref="SendAsync"/>.</summary>
    public void DeliveryFail(PlayerEntity pc)
    {
        _logger.LogInformation("mail_deliveryfail: clearing draft for {Pc}", pc.Name);
        Clear(pc);
    }

    /// <inheritdoc />
    public async Task<bool> GetAttachmentAsync(PlayerEntity pc, int mailId, CancellationToken ct = default)
    {
        var session = _sessions?.GetByEntityId(pc.Id);
        var inv = session?.Inventory;
        if (session?.CharacterData == null || inv == null || _mailIpc == null) return false;

        var resp = await _mailIpc.MailGetAttachmentAsync(pc.AccountId, pc.CharacterId, mailId, ct);
        if (resp is not { Success: true }) return false;

        // Inventory-full / can't-hold gate (rAthena rejects, leaving the mail unclaimed).
        var freshSlots = 0;
        foreach (var a in resp.Items)
            if (FindMergeable(inv, a) == null) freshSlots++;
        if (inv.Count + freshSlots > MaxInventory) return false;

        // Overweight gate (rAthena mail_getattachment MAIL_ATTACH_WEIGHT): the incoming items must
        // not push the player past the weight cap.
        if (CurrentWeight(inv) + IncomingWeight(resp.Items) > MaxWeight(pc)) return false;

        foreach (var a in resp.Items) CreditItem(session, inv, a);
        if (resp.Zeny > 0)
            session.CharacterData.Zeny = (uint)Math.Min(MaxDraftZeny, (long)session.CharacterData.Zeny + resp.Zeny);
        // PACKET-06: ZC_ACK_MAIL_GET_ITEM emit seam. Char side flips the mail's attachment-claimed flag.
        _logger.LogInformation("mail_getattachment: {Pc} claimed mail #{Mail} ({N} items, {Zeny}z)",
            pc.Name, mailId, resp.Items.Count, resp.Zeny);
        return true;
    }

    /// <summary>rAthena <c>mail_refresh_remaining_amount</c> — re-request the inbox so the client's
    /// remaining-mail counter is refreshed. The list render is PACKET-06.</summary>
    public void RefreshRemainingAmount(PlayerEntity pc)
    {
        if (_mailIpc == null) return;
        _ = _mailIpc.MailRequestInboxAsync(pc.AccountId, pc.CharacterId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MailMessageData>> RequestInboxAsync(PlayerEntity pc, CancellationToken ct = default)
    {
        if (_mailIpc == null) return Array.Empty<MailMessageData>();
        var resp = await _mailIpc.MailRequestInboxAsync(pc.AccountId, pc.CharacterId, ct);
        if (resp is not { Success: true }) return Array.Empty<MailMessageData>();
        _logger.LogInformation("mail_requestinbox: {Pc} has {N} mail(s)", pc.Name, resp.Mails.Count);
        return resp.Mails;
    }

    /// <inheritdoc />
    public async Task<MailMessageData?> ReadMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
    {
        if (!pc.MailOpened || _mailIpc == null) return null;
        var resp = await _mailIpc.MailReadAsync(pc.AccountId, pc.CharacterId, mailId, ct);
        if (resp is not { Success: true } || resp.Mail == null) return null;
        return resp.Mail;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
    {
        if (!pc.MailOpened || _mailIpc == null) return false;
        // rAthena gates delete on the attachments being claimed first; the char side is the
        // authority (it knows the mail's current attach state) — delegate + honor its verdict.
        var resp = await _mailIpc.MailDeleteAsync(pc.AccountId, pc.CharacterId, mailId, ct);
        var ok = resp is { Success: true };
        _logger.LogInformation("mail_delete: {Pc} delete mail #{Mail} = {Ok}", pc.Name, mailId, ok);
        return ok;
    }

    // --- weight helpers (mirror the cash-shop / PlayerWeightStatusService pattern) ---

    private long CurrentWeight(IReadOnlyList<InventoryItem> inv)
    {
        if (_items == null) return 0;
        long w = 0;
        foreach (var row in inv)
        {
            if (row.Amount == 0) continue;
            w += (long)(_items.Get(row.NameId)?.Weight ?? 0) * row.Amount;
        }
        return w;
    }

    private long IncomingWeight(IReadOnlyList<MailAttachmentItem> items)
    {
        if (_items == null) return 0;
        long w = 0;
        foreach (var a in items)
            w += (long)(_items.Get(a.NameId)?.Weight ?? 0) * a.Amount;
        return w;
    }

    private static long MaxWeight(PlayerEntity pc) => BaseMaxWeight + Math.Max(0, pc.EquipBonuses.AddMaxWeight);

    // --- inventory helpers (mirror the Trade debit/credit-with-fidelity pattern) ---

    private static InventoryItem? FindBySlot(List<InventoryItem> inv, int serverIndex)
    {
        foreach (var i in inv) if (i.ServerIndex == serverIndex) return i;
        return null;
    }

    private static MailAttachmentItem ToAttachment(InventoryItem i, int amount)
    {
        var a = new MailAttachmentItem
        {
            Index = (uint)i.ServerIndex,
            NameId = i.NameId,
            Amount = (uint)amount,
            Refine = i.Refine,
            Attribute = i.Attribute,
            Identify = i.Identified ? 1 : 0,
            Card0 = i.Card0, Card1 = i.Card1, Card2 = i.Card2, Card3 = i.Card3,
            UniqueId = i.UniqueId,
            Bound = i.Bound,
            EnchantGrade = i.EnchantGrade,
        };
        WriteOptions(a, i.Options);
        return a;
    }

    /// <summary>Copy the 5 random-option slots into the attachment payload (rAthena item.option[]).</summary>
    private static void WriteOptions(MailAttachmentItem a, ItemOption[] opt)
    {
        if (opt.Length > 0) { a.OptionId0 = opt[0].Id; a.OptionVal0 = opt[0].Value; a.OptionParm0 = opt[0].Param; }
        if (opt.Length > 1) { a.OptionId1 = opt[1].Id; a.OptionVal1 = opt[1].Value; a.OptionParm1 = opt[1].Param; }
        if (opt.Length > 2) { a.OptionId2 = opt[2].Id; a.OptionVal2 = opt[2].Value; a.OptionParm2 = opt[2].Param; }
        if (opt.Length > 3) { a.OptionId3 = opt[3].Id; a.OptionVal3 = opt[3].Value; a.OptionParm3 = opt[3].Param; }
        if (opt.Length > 4) { a.OptionId4 = opt[4].Id; a.OptionVal4 = opt[4].Value; a.OptionParm4 = opt[4].Param; }
    }

    private static ItemOption[] ReadOptions(MailAttachmentItem a) => new[]
    {
        new ItemOption { Id = (short)a.OptionId0, Value = (short)a.OptionVal0, Param = (sbyte)a.OptionParm0 },
        new ItemOption { Id = (short)a.OptionId1, Value = (short)a.OptionVal1, Param = (sbyte)a.OptionParm1 },
        new ItemOption { Id = (short)a.OptionId2, Value = (short)a.OptionVal2, Param = (sbyte)a.OptionParm2 },
        new ItemOption { Id = (short)a.OptionId3, Value = (short)a.OptionVal3, Param = (sbyte)a.OptionParm3 },
        new ItemOption { Id = (short)a.OptionId4, Value = (short)a.OptionVal4, Param = (sbyte)a.OptionParm4 },
    };

    private static void DebitSlot(Map.Server.MapSessionData session, List<InventoryItem> inv, int serverIndex, int amount)
    {
        var item = FindBySlot(inv, serverIndex);
        if (item == null) return;
        item.Amount -= (uint)amount;
        if (item.Amount == 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.Remove(item);
        }
    }

    private static InventoryItem? FindMergeable(List<InventoryItem> inv, MailAttachmentItem a)
    {
        foreach (var i in inv)
        {
            if (i.NameId != a.NameId) continue;
            if (i.Refine != a.Refine || (i.Identified ? 1 : 0) != a.Identify) continue;
            if (i.Card0 != a.Card0 || i.Card1 != a.Card1 || i.Card2 != a.Card2 || i.Card3 != a.Card3) continue;
            return i;
        }
        return null;
    }

    private static void CreditItem(Map.Server.MapSessionData session, List<InventoryItem> inv, MailAttachmentItem a)
    {
        var mergeable = FindMergeable(inv, a);
        if (mergeable != null)
        {
            mergeable.Amount += a.Amount;
            return;
        }
        var nextSlot = 0;
        foreach (var i in inv) if (i.ServerIndex >= nextSlot) nextSlot = i.ServerIndex + 1;
        inv.Add(new InventoryItem
        {
            ServerIndex = nextSlot,
            NameId = a.NameId,
            Amount = a.Amount,
            Refine = (byte)a.Refine,
            Attribute = (byte)a.Attribute,
            Identified = a.Identify != 0,
            Card0 = a.Card0, Card1 = a.Card1, Card2 = a.Card2, Card3 = a.Card3,
            UniqueId = a.UniqueId,
            Bound = (byte)a.Bound,
            EnchantGrade = (byte)a.EnchantGrade,
            Options = ReadOptions(a),
        });
    }
}
