using System.Collections.Concurrent;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Handlers.Mail;
using Map.Server.Mail;
using Map.Server.Session;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mail;

/// <summary>
/// GP-MAIL — RODEX manage-action packet handlers (delete + get-attachment) → service → ZC ack.
/// </summary>
public class MailHandlersTests
{
    [Fact]
    public async Task Delete_success_emits_ack()
    {
        var (reg, mail, pc, session) = Build();
        mail.DeleteOk = true;
        var h = new MailDeleteHandler(reg, mail, NullLogger<MailDeleteHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_DELETE_MAIL(), ("OpenType", (byte)0), ("MailId", 42L)));

        Assert.Equal(42L, mail.LastDeleteId);
        Assert.Contains((ushort)PacketHeader.ZC_ACK_DELETE_MAIL, SentIds(session));
    }

    [Fact]
    public async Task Delete_refused_emits_nothing()
    {
        var (reg, mail, pc, session) = Build();
        mail.DeleteOk = false;
        var h = new MailDeleteHandler(reg, mail, NullLogger<MailDeleteHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_DELETE_MAIL(), ("OpenType", (byte)0), ("MailId", 42L)));

        Assert.DoesNotContain((ushort)PacketHeader.ZC_ACK_DELETE_MAIL, SentIds(session));
    }

    [Fact]
    public async Task GetItem_success_acks_result_0()
    {
        var (reg, mail, pc, session) = Build();
        mail.GetOk = true;
        var h = new MailGetItemHandler(reg, mail, NullLogger<MailGetItemHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_ITEM_FROM_MAIL(), ("MailId", 7L), ("OpenType", (byte)0)));

        Assert.Equal(7, mail.LastGetId);
        AssertAck(session, PacketHeader.ZC_ACK_ITEM_FROM_MAIL, expectedResult: 0);
    }

    [Fact]
    public async Task GetItem_failure_acks_result_1()
    {
        var (reg, mail, pc, session) = Build();
        mail.GetOk = false; // e.g. inventory full / overweight
        var h = new MailGetItemHandler(reg, mail, NullLogger<MailGetItemHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_ITEM_FROM_MAIL(), ("MailId", 7L), ("OpenType", (byte)0)));

        AssertAck(session, PacketHeader.ZC_ACK_ITEM_FROM_MAIL, expectedResult: 1);
    }

    [Fact]
    public async Task GetZeny_success_acks_result_0()
    {
        var (reg, mail, pc, session) = Build();
        mail.GetOk = true;
        var h = new MailGetZenyHandler(reg, mail, NullLogger<MailGetZenyHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_ZENY_FROM_MAIL(), ("MailId", 9L), ("OpenType", (byte)0)));

        AssertAck(session, PacketHeader.ZC_ACK_ZENY_FROM_MAIL, expectedResult: 0);
    }

    [Fact]
    public async Task Open_emits_inbox_list_and_sets_opened()
    {
        var (reg, mail, pc, session) = Build();
        mail.Inbox.Add(new MailMessageData { MailId = 10, SenderName = "Alice", SenderAccountId = 9, Title = "Hello", Zeny = 500, Opened = false });
        var withItem = new MailMessageData { MailId = 11, SenderName = "Bob", SenderAccountId = 8, Title = "Hi", Opened = true };
        withItem.Items.Add(new MailAttachmentItem { NameId = 501, Amount = 1 });
        mail.Inbox.Add(withItem);
        var h = new MailOpenHandler(reg, mail, NullLogger<MailOpenHandler>.Instance);

        await h.HandleAsync(session, new CZ_OPEN_MAILBOX());

        Assert.True(mail.Opened);
        // The outbound queue stores already-serialized wire bytes.
        var bytes = Outbound(session).Single(b => (ushort)(b[0] | (b[1] << 8)) == (ushort)PacketHeader.ZC_ACK_MAIL_LIST);
        // header(2) + len(2) + unknown(1) then per-mail. Verify header/len + the first mail's fields.
        Assert.Equal(bytes.Length, bytes[2] | (bytes[3] << 8));   // length field == actual size
        Assert.Equal(1, bytes[4]);                                // "unknown" = 1
        // first mail entry starts at byte 5: type.B id.Q read.B flags.B sender.24 deletion.L titleLen.W title
        Assert.Equal(0, bytes[5]);                                // type = normal
        Assert.Equal(10L, BitConverter.ToInt64(bytes, 6));        // mailId
        Assert.Equal(0, bytes[14]);                               // read = false
        Assert.Equal(2, bytes[15]);                               // flags = ZENY (0x2)
        Assert.Equal("Alice", ReadCString(bytes, 16, 24));        // sender (24-byte field)
    }

    [Fact]
    public async Task Read_emits_read_window_with_body_zeny_and_item()
    {
        var (reg, mail, pc, session) = Build();
        var msg = new MailMessageData { MailId = 77, SenderName = "Alice", Title = "T", Body = "Hello there", Zeny = 1234, Opened = false };
        msg.Items.Add(new MailAttachmentItem { NameId = 501, Amount = 3, Identify = 1, Refine = 5, Card0 = 4001 });
        mail.ReadResult = msg;
        var h = new MailReadHandler(reg, mail, NullLogger<MailReadHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_READ_MAIL(), ("OpenType", (byte)0), ("MailId", 77L)));

        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_ACK_READ_RODEX);
        Assert.Equal(b.Length, b[2] | (b[3] << 8));               // length field == actual size
        Assert.Equal(0, b[4]);                                    // opentype
        Assert.Equal(77L, BitConverter.ToInt64(b, 5));            // mailId
        var textLen = BitConverter.ToUInt16(b, 13);
        Assert.Equal(1234L, BitConverter.ToInt64(b, 15));         // zeny
        Assert.Equal(1, b[23]);                                   // itemCnt
        Assert.Equal("Hello there", ReadCString(b, 24, textLen)); // body (null-terminated)
        var io = 24 + textLen;                                    // first item sub begins after the body
        Assert.Equal(3, BitConverter.ToInt16(b, io));             // count
        Assert.Equal(501u, BitConverter.ToUInt32(b, io + 2));     // itemId
    }

    [Fact]
    public async Task BeginWrite_clears_draft_and_acks()
    {
        var (reg, mail, pc, session) = Build();
        var h = new MailBeginWriteHandler(reg, mail);

        await h.HandleAsync(session, Cz(new CZ_REQ_OPEN_WRITE_MAIL(), ("ReceiveName", "Bob")));

        Assert.True(mail.Opened);
        Assert.True(mail.Cleared);
        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_ACK_OPEN_WRITE_MAIL);
        Assert.Equal(1, b[26]);                          // result = ok (after header(2) + name(24))
        Assert.Equal("Bob", ReadCString(b, 2, 24));      // echoed receive name
    }

    [Fact]
    public async Task CheckName_returns_charid_when_found_and_zero_when_not()
    {
        var (reg, mail, pc, session) = Build();
        mail.CheckFound = true; mail.CheckCharId = 4242;
        var h = new MailCheckNameHandler(reg, mail);

        await h.HandleAsync(session, Cz(new CZ_CHECKNAME(), ("Name", "Alice")));
        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_CHECKNAME);
        Assert.Equal(4242, BitConverter.ToInt32(b, 2));

        var (reg2, mail2, _, s2) = Build();
        mail2.CheckFound = false;
        await new MailCheckNameHandler(reg2, mail2).HandleAsync(s2, Cz(new CZ_CHECKNAME(), ("Name", "Ghost")));
        var b2 = Outbound(s2).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_CHECKNAME);
        Assert.Equal(0, BitConverter.ToInt32(b2, 2));    // not found
    }

    [Fact]
    public async Task Send_pushes_zeny_and_acks_result()
    {
        var (reg, mail, pc, session) = Build();
        mail.SendOk = true;
        var h = new MailSendHandler(reg, mail, NullLogger<MailSendHandler>.Instance);

        await h.HandleAsync(session, Cz(new CZ_REQ_SEND_MAIL(), ("Receiver", "Bob"), ("Zeny", 5000L), ("Title", "Hi"), ("Body", "yo")));

        Assert.Equal("Bob", mail.LastSendTo);
        Assert.Equal(5000L, mail.LastSendZeny);          // the packet zeny reached the draft
        var b = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_WRITE_MAIL_RESULT);
        Assert.Equal(0, b[2]);                           // success

        mail.SendOk = false;
        ClearOut(session);
        await h.HandleAsync(session, Cz(new CZ_REQ_SEND_MAIL(), ("Receiver", "Bob"), ("Zeny", 0L), ("Title", "Hi"), ("Body", "yo")));
        var bf = Outbound(session).Single(x => (ushort)(x[0] | (x[1] << 8)) == (ushort)PacketHeader.ZC_WRITE_MAIL_RESULT);
        Assert.Equal(1, bf[2]);                          // failed
    }

    private static void ClearOut(MapSessionData session)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f?.GetValue(session) is System.Collections.Concurrent.ConcurrentQueue<byte[]> q) q.Clear();
    }

    [Fact]
    public void ReadWindow_packet_size_matches_written_bytes()
    {
        var msg = new MailMessageData { MailId = 1, Body = "body", Zeny = 5 };
        msg.Items.Add(new MailAttachmentItem { NameId = 909, Amount = 1 });
        msg.Items.Add(new MailAttachmentItem { NameId = 910, Amount = 2 });
        var p = MailReadHandler.BuildRead(0, msg, catalog: null);
        Assert.Equal(p.GetSize(), Serialize(p).Length);
    }

    [Fact]
    public async Task Refresh_resends_the_list()
    {
        var (reg, mail, pc, session) = Build();
        mail.Inbox.Add(new MailMessageData { MailId = 1, SenderName = "X", Title = "A" });
        var h = new MailRefreshHandler(reg, mail, NullLogger<MailRefreshHandler>.Instance);

        await h.HandleAsync(session, new CZ_REQ_REFRESH_MAIL_LIST());

        Assert.Contains((ushort)PacketHeader.ZC_ACK_MAIL_LIST, SentIds(session));
    }

    [Fact]
    public void MailList_packet_size_matches_written_bytes()
    {
        var list = MailOpenHandler.BuildList(new[]
        {
            new MailMessageData { MailId = 1, SenderName = "X", Title = "TitleA" },
            new MailMessageData { MailId = 2, SenderName = "LongerName", Title = "Another title here" },
        });
        var bytes = Serialize(list);
        Assert.Equal(list.GetSize(), bytes.Length);               // GetSize is exact (no over/under-run)
    }

    private static byte[] Serialize(Core.Server.Packets.OutgoingPacket p)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        p.WritePacket(w);
        w.Flush();
        return ms.ToArray();
    }

    private static string ReadCString(byte[] b, int off, int width)
    {
        var end = off;
        while (end < off + width && b[end] != 0) end++;
        return System.Text.Encoding.ASCII.GetString(b, off, end - off);
    }

    // --- helpers ---

    private static (FakeEntities reg, FakeMail mail, PlayerEntity pc, MapSessionData session) Build()
    {
        var reg = new FakeEntities();
        var pc = new PlayerEntity(1, 7, "Pc", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1, MailOpened = true };
        reg.Add(pc);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 7, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        return (reg, new FakeMail(), pc, session);
    }

    private static T Cz<T>(T packet, params (string prop, object val)[] fields) where T : IncomingPacket
    {
        foreach (var (prop, val) in fields)
            typeof(T).GetProperty(prop)!.SetValue(packet, val);
        return packet;
    }

    private static ushort[] SentIds(MapSessionData session) => Outbound(session).Select(b => (ushort)(b[0] | (b[1] << 8))).ToArray();

    private static void AssertAck(MapSessionData session, PacketHeader header, byte expectedResult)
    {
        var packet = Outbound(session).Single(b => (ushort)(b[0] | (b[1] << 8)) == (ushort)header);
        // ZC_ACK_*_FROM_MAIL: header(2) + MailID(8) + opentype(1) + result(1) → result at byte 11.
        Assert.Equal(expectedResult, packet[11]);
    }

    private static IReadOnlyList<byte[]> Outbound(MapSessionData session)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(session) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FakeMail : IMailService
    {
        public bool DeleteOk; public long LastDeleteId;
        public bool GetOk; public int LastGetId;
        public readonly List<MailMessageData> Inbox = new();
        public bool Opened; public bool Cleared;
        public MailMessageData? ReadResult;
        public bool SendOk; public string? LastSendTo; public long LastSendZeny;
        public bool CheckFound; public long CheckCharId;

        public Task<bool> DeleteMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
        { LastDeleteId = mailId; return Task.FromResult(DeleteOk); }
        public Task<bool> GetAttachmentAsync(PlayerEntity pc, int mailId, CancellationToken ct = default)
        { LastGetId = mailId; return Task.FromResult(GetOk); }
        public Task<(bool Found, long CharId)> CheckReceiverAsync(string name, CancellationToken ct = default)
            => Task.FromResult((CheckFound, CheckCharId));

        public int OpenMail(PlayerEntity pc) { Opened = true; return 0; }
        public void Clear(PlayerEntity pc) { Cleared = true; }
        public Task<bool> SendAsync(PlayerEntity pc, string recipientName, string title, string body, CancellationToken ct = default)
        { LastSendTo = recipientName; LastSendZeny = pc.MailDraftZeny; return Task.FromResult(SendOk); }
        public bool SetAttachment(PlayerEntity pc, int inventoryIndex, int amount) => false;
        public bool RemoveItem(PlayerEntity pc, int inventoryIndex) => false;
        public bool RemoveZeny(PlayerEntity pc, long amount) => false;
        public bool InvalidOperation(PlayerEntity pc) => false;
        public void DeliveryFail(PlayerEntity pc) { }
        public void RefreshRemainingAmount(PlayerEntity pc) { }
        public Task<IReadOnlyList<MailMessageData>> RequestInboxAsync(PlayerEntity pc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MailMessageData>>(Inbox);
        public Task<MailMessageData?> ReadMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
            => Task.FromResult(ReadResult);
    }

    private sealed class FakeEntities : IEntityRegistry
    {
        private readonly Dictionary<int, Entity> _e = new();
        public void Add(Entity entity) => _e[entity.Id.Value] = entity;
        public Entity? Remove(EntityId id) { _e.Remove(id.Value, out var e); return e; }
        public Entity? Get(EntityId id) => _e.GetValueOrDefault(id.Value);
        public bool Contains(EntityId id) => _e.ContainsKey(id.Value);
        public void Move(EntityId id, short newX, short newY) { }
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask) => Array.Empty<Entity>();
        public IEnumerable<Entity> All() => _e.Values;
        public int Count => _e.Count;
    }
}
