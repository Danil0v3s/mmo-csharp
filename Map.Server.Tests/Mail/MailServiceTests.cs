using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mail;
using Map.Server.Services;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mail;

/// <summary>
/// FEATURE-05 — mail service debit / dispatch / credit / rebound state machine.
/// </summary>
public class MailServiceTests
{
    private const uint PotionId = 501;

    private static (MailService svc, PlayerEntity pc, MapSessionData session, FakeMailIpc ipc) Build(
        uint zeny = 100_000, Map.Server.Items.IItemCatalog? items = null, params InventoryItem[] inv)
    {
        var pc = new PlayerEntity(1, 7, "Sender", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 7, CharacterId = 1, EntityId = pc.Id,
            Inventory = inv.ToList(),
            CharacterData = new CharacterDataResponse { Zeny = zeny },
        };
        var sessions = new FakeSessions(session);
        var ipc = new FakeMailIpc();
        var svc = new MailService(NullLogger<MailService>.Instance, sessions, ipc, items);
        pc.MailOpened = true;
        return (svc, pc, session, ipc);
    }

    private static InventoryItem Item(int slot, uint amount, byte refine = 0) =>
        new() { Id = slot + 1, ServerIndex = slot, NameId = PotionId, Amount = amount, Refine = refine, Identified = true };

    [Fact]
    public async Task Send_debits_items_zeny_and_dispatches_attachment()
    {
        var (svc, pc, session, ipc) = Build(zeny: 100_000, inv: Item(0, 5));
        svc.SetAttachment(pc, 0, 3);
        svc.RemoveZeny(pc, -1000); // attach 1000z

        var ok = await svc.SendAsync(pc, "Receiver", "Hi", "Body");

        Assert.True(ok);
        Assert.Equal(2u, session.Inventory!.Single().Amount);       // 5 - 3 debited
        Assert.Equal(100_000u - (1000u + 2500u), session.CharacterData!.Zeny); // attached zeny + 1*price fee
        Assert.Empty(pc.MailDraftItems);                             // draft cleared
        Assert.Equal(1, ipc.SendCalls);
        Assert.Single(ipc.LastItems);
        Assert.Equal(3u, ipc.LastItems[0].Amount);
        Assert.Equal(1000, ipc.LastZeny);                           // recipient receives the attached zeny
    }

    [Fact]
    public async Task Send_insufficient_zeny_returns_false_and_keeps_draft()
    {
        var (svc, pc, session, ipc) = Build(zeny: 100, inv: Item(0, 5));
        svc.SetAttachment(pc, 0, 1);
        svc.RemoveZeny(pc, -1000); // attach 1000z, but only 100 on hand

        var ok = await svc.SendAsync(pc, "Receiver", "Hi", "Body");

        Assert.False(ok);
        Assert.Equal(5u, session.Inventory!.Single().Amount); // not debited
        Assert.Equal(100u, session.CharacterData!.Zeny);
        Assert.Single(pc.MailDraftItems);                     // draft intact
        Assert.Equal(0, ipc.SendCalls);
    }

    [Fact]
    public async Task Send_missing_attachment_item_returns_false()
    {
        var (svc, pc, _, ipc) = Build(zeny: 100_000, inv: Item(0, 2));
        svc.SetAttachment(pc, 0, 5); // only 2 on hand

        Assert.False(await svc.SendAsync(pc, "Receiver", "Hi", "Body"));
        Assert.Equal(0, ipc.SendCalls);
    }

    [Fact]
    public async Task Send_to_self_returns_false()
    {
        var (svc, pc, _, ipc) = Build(inv: Item(0, 5));
        svc.SetAttachment(pc, 0, 1);
        Assert.False(await svc.SendAsync(pc, "Sender", "Hi", "Body")); // recipient == own name
        Assert.Equal(0, ipc.SendCalls);
    }

    [Fact]
    public async Task Send_dispatch_failure_rebounds_items_and_zeny()
    {
        var (svc, pc, session, ipc) = Build(zeny: 100_000, inv: Item(0, 5));
        ipc.FailSend = true;
        svc.SetAttachment(pc, 0, 3);
        svc.RemoveZeny(pc, -1000);

        var ok = await svc.SendAsync(pc, "Receiver", "Hi", "Body");

        Assert.False(ok);
        Assert.Equal(5u, session.Inventory!.Single().Amount);  // rebounded
        Assert.Equal(100_000u, session.CharacterData!.Zeny);   // rebounded
    }

    [Fact]
    public async Task GetAttachment_credits_items_and_zeny()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1000, inv: Item(0, 1));
        ipc.AttachZeny = 500;
        ipc.AttachItems.Add(new MailAttachmentItem { NameId = 909, Amount = 2, Identify = 1 });

        var ok = await svc.GetAttachmentAsync(pc, mailId: 42);

        Assert.True(ok);
        Assert.Equal(1500u, session.CharacterData!.Zeny);
        Assert.Contains(session.Inventory!, i => i.NameId == 909 && i.Amount == 2);
    }

    [Fact]
    public void DeliveryFail_clears_draft_without_item_loss()
    {
        var (svc, pc, session, _) = Build(inv: Item(0, 5));
        svc.SetAttachment(pc, 0, 3);
        svc.DeliveryFail(pc);
        Assert.Empty(pc.MailDraftItems);
        Assert.Equal(5u, session.Inventory!.Single().Amount); // never debited
    }

    [Fact]
    public async Task GetAttachment_over_weight_is_rejected_without_mutation()
    {
        var heavy = new FakeItems((909, weight: 30000)); // one unit already exceeds the 20000 cap
        var (svc, pc, session, ipc) = Build(zeny: 1000, items: heavy, inv: Item(0, 1));
        ipc.AttachZeny = 500;
        ipc.AttachItems.Add(new MailAttachmentItem { NameId = 909, Amount = 1, Identify = 1 });

        var ok = await svc.GetAttachmentAsync(pc, mailId: 42);

        Assert.False(ok);
        Assert.Equal(1000u, session.CharacterData!.Zeny);                 // zeny not credited
        Assert.DoesNotContain(session.Inventory!, i => i.NameId == 909);  // item not credited
    }

    [Fact]
    public async Task RequestInbox_returns_the_mailbox()
    {
        var (svc, pc, _, ipc) = Build();
        ipc.Inbox.Add(new MailMessageData { MailId = 1, Title = "A", SenderName = "X" });
        ipc.Inbox.Add(new MailMessageData { MailId = 2, Title = "B", SenderName = "Y" });

        var inbox = await svc.RequestInboxAsync(pc);

        Assert.Equal(2, inbox.Count);
        Assert.Equal("A", inbox[0].Title);
    }

    [Fact]
    public async Task ReadMail_returns_the_body_when_open()
    {
        var (svc, pc, _, ipc) = Build();
        ipc.ReadResult = new MailMessageData { MailId = 7, Title = "Hi", Body = "Hello", SenderName = "X" };

        var mail = await svc.ReadMailAsync(pc, 7);

        Assert.NotNull(mail);
        Assert.Equal("Hello", mail!.Body);

        pc.MailOpened = false;
        Assert.Null(await svc.ReadMailAsync(pc, 7)); // gated on the mailbox being open
    }

    [Fact]
    public async Task DeleteMail_delegates_to_char_side()
    {
        var (svc, pc, _, ipc) = Build();
        ipc.DeleteOk = true;
        Assert.True(await svc.DeleteMailAsync(pc, 5));

        ipc.DeleteOk = false;
        Assert.False(await svc.DeleteMailAsync(pc, 5)); // char-side refusal honored
    }

    // --- fakes ---

    private sealed class FakeItems : Map.Server.Items.IItemCatalog
    {
        private readonly Dictionary<uint, Core.Database.Entities.ItemEntity> _byId = new();
        public FakeItems(params (uint id, ushort weight)[] items)
        {
            foreach (var (id, weight) in items)
                _byId[id] = new Core.Database.Entities.ItemEntity { Id = id, NameAegis = $"i{id}", Weight = weight, Type = "Etc" };
        }
        public int Count => _byId.Count;
        public Core.Database.Entities.ItemEntity? Get(uint itemId) => _byId.GetValueOrDefault(itemId);
        public Core.Database.Entities.ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => _byId.Values;
        public void Reload() { }
    }

    private sealed class FakeSessions(MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => session;
    }

    private sealed class FakeMailIpc : ICharServerIpcServiceMail
    {
        public int SendCalls; public IReadOnlyList<MailAttachmentItem> LastItems = Array.Empty<MailAttachmentItem>();
        public long LastZeny; public bool FailSend;
        public long AttachZeny; public readonly List<MailAttachmentItem> AttachItems = new();
        public readonly List<MailMessageData> Inbox = new();
        public MailMessageData? ReadResult; public bool DeleteOk;

        public Task<MailReceiverCheckResponse?> MailReceiverCheckAsync(string receiverName, CancellationToken ct = default)
            => Task.FromResult<MailReceiverCheckResponse?>(new MailReceiverCheckResponse
            { Success = true, AccountId = 9, CharacterId = 2, ReceiverName = receiverName });

        public Task<MailSendResponse?> MailSendAsync(int senderAccountId, long senderCharacterId, string senderName,
            int receiverAccountId, long receiverCharacterId, string receiverName, string title, string body,
            long zeny, byte[] attachment, IReadOnlyList<MailAttachmentItem>? items = null, CancellationToken ct = default)
        {
            SendCalls++; LastItems = items ?? Array.Empty<MailAttachmentItem>(); LastZeny = zeny;
            return Task.FromResult<MailSendResponse?>(new MailSendResponse { Success = !FailSend, MailId = 1 });
        }

        public Task<MailGetAttachmentResponse?> MailGetAttachmentAsync(int accountId, long characterId, long mailId, CancellationToken ct = default)
        {
            var resp = new MailGetAttachmentResponse { Success = true, Zeny = AttachZeny };
            resp.Items.AddRange(AttachItems);
            return Task.FromResult<MailGetAttachmentResponse?>(resp);
        }

        public Task<MailRequestInboxResponse?> MailRequestInboxAsync(int accountId, long characterId, CancellationToken ct = default)
        {
            var resp = new MailRequestInboxResponse { Success = true };
            resp.Mails.AddRange(Inbox);
            return Task.FromResult<MailRequestInboxResponse?>(resp);
        }
        public Task<MailReadResponse?> MailReadAsync(int accountId, long characterId, long mailId, CancellationToken ct = default)
            => Task.FromResult<MailReadResponse?>(new MailReadResponse { Success = ReadResult != null, Mail = ReadResult });
        public Task<MailDeleteResponse?> MailDeleteAsync(int accountId, long characterId, long mailId, CancellationToken ct = default)
            => Task.FromResult<MailDeleteResponse?>(new MailDeleteResponse { Success = DeleteOk });
        public Task<MailReturnResponse?> MailReturnAsync(int accountId, long characterId, long mailId, CancellationToken ct = default)
            => Task.FromResult<MailReturnResponse?>(new MailReturnResponse());
    }
}
