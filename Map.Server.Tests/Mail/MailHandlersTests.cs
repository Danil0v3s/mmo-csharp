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

        public Task<bool> DeleteMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
        { LastDeleteId = mailId; return Task.FromResult(DeleteOk); }
        public Task<bool> GetAttachmentAsync(PlayerEntity pc, int mailId, CancellationToken ct = default)
        { LastGetId = mailId; return Task.FromResult(GetOk); }

        public int OpenMail(PlayerEntity pc) => 0;
        public void Clear(PlayerEntity pc) { }
        public Task<bool> SendAsync(PlayerEntity pc, string recipientName, string title, string body, CancellationToken ct = default) => Task.FromResult(false);
        public bool SetAttachment(PlayerEntity pc, int inventoryIndex, int amount) => false;
        public bool RemoveItem(PlayerEntity pc, int inventoryIndex) => false;
        public bool RemoveZeny(PlayerEntity pc, long amount) => false;
        public bool InvalidOperation(PlayerEntity pc) => false;
        public void DeliveryFail(PlayerEntity pc) { }
        public void RefreshRemainingAmount(PlayerEntity pc) { }
        public Task<IReadOnlyList<MailMessageData>> RequestInboxAsync(PlayerEntity pc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MailMessageData>>(Array.Empty<MailMessageData>());
        public Task<MailMessageData?> ReadMailAsync(PlayerEntity pc, long mailId, CancellationToken ct = default)
            => Task.FromResult<MailMessageData?>(null);
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
