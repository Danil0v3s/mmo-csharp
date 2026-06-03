using System.Collections.Concurrent;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

/// <summary>
/// GP-PARTY — party dot/HP sync (rAthena party_send_xy_timer + clif_party_hp): coarse ~1s broadcast
/// of position + HP to same-map party members, change-gated.
/// </summary>
public class PartySyncServiceTests
{
    [Fact]
    public void Tick_broadcasts_position_and_hp_to_other_party_members()
    {
        var c = Build();
        c.Svc.Tick(1000);

        // Alice's client received Bob's position + HP; Bob's received Alice's.
        Assert.Contains((ushort)PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM, Sent(c.AliceSession));
        Assert.Contains((ushort)PacketHeader.ZC_NOTIFY_HP_TO_GROUPM, Sent(c.AliceSession));
        Assert.Contains((ushort)PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM, Sent(c.BobSession));
        Assert.Contains((ushort)PacketHeader.ZC_NOTIFY_HP_TO_GROUPM, Sent(c.BobSession));
    }

    [Fact]
    public void Second_tick_with_no_change_sends_nothing()
    {
        var c = Build();
        c.Svc.Tick(1000);
        Clear(c.AliceSession); Clear(c.BobSession);

        c.Svc.Tick(2000); // past the 1s gate, but nothing moved/changed
        Assert.Empty(Sent(c.AliceSession));
        Assert.Empty(Sent(c.BobSession));
    }

    [Fact]
    public void Hp_change_rebroadcasts_hp()
    {
        var c = Build();
        c.Svc.Tick(1000);
        Clear(c.AliceSession);

        c.Bob.Hp = 30; // Bob took damage
        c.Svc.Tick(2000);

        Assert.Contains((ushort)PacketHeader.ZC_NOTIFY_HP_TO_GROUPM, Sent(c.AliceSession)); // Alice sees Bob's new HP
        Assert.DoesNotContain((ushort)PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM, Sent(c.AliceSession)); // Bob didn't move
    }

    [Fact]
    public void Gate_suppresses_ticks_within_the_interval()
    {
        var c = Build();
        c.Svc.Tick(1000);
        Clear(c.AliceSession);
        c.Bob.Hp = 30;

        c.Svc.Tick(1500); // < 1000 + 1000 interval → gated
        Assert.Empty(Sent(c.AliceSession));
    }

    // --- helpers ---

    private sealed record Ctx(PartySyncService Svc, PlayerEntity Alice, PlayerEntity Bob,
        MapSessionData AliceSession, MapSessionData BobSession);

    private static Ctx Build()
    {
        var reg = new FakeEntities();
        var sessions = new FakeSessions();
        var alice = NewPc(1, 100, "Alice", x: 10, y: 10, hp: 50); reg.Add(alice);
        var bob = NewPc(2, 200, "Bob", x: 20, y: 20, hp: 80); reg.Add(bob);
        var aS = NewSession(alice); sessions.Register(alice.Id, aS);
        var bS = NewSession(bob); sessions.Register(bob.Id, bS);
        var partyMap = new FakePartyMap(reg);
        var svc = new PartySyncService(reg, partyMap, sessions, NullLogger<PartySyncService>.Instance);
        return new Ctx(svc, alice, bob, aS, bS);
    }

    private static PlayerEntity NewPc(int charId, int acc, string name, short x, short y, int hp)
        => new(charId, acc, name, Guid.NewGuid(), 1, x, y) { Hp = hp, MaxHp = 100, PartyId = 42 };

    private static MapSessionData NewSession(PlayerEntity pc)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = pc.AccountId, CharacterId = pc.CharacterId, EntityId = pc.Id };
    }

    private static ushort[] Sent(MapSessionData s) => Outbound(s).Select(b => (ushort)(b[0] | (b[1] << 8))).ToArray();
    private static void Clear(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f?.GetValue(s) is ConcurrentQueue<byte[]> q) q.Clear();
    }
    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FakePartyMap(FakeEntities reg) : IPartyMapService
    {
        public int ForEachOnSameMap(PlayerEntity origin, Action<PlayerEntity> callback, bool includeSelf = true)
        {
            var n = 0;
            foreach (var e in reg.All())
                if (e is PlayerEntity p && p.PartyId == origin.PartyId && p.MapId == origin.MapId
                    && (includeSelf || p.CharacterId != origin.CharacterId))
                { callback(p); n++; }
            return n;
        }
        public int ForEachOnSameMapInRange(PlayerEntity origin, short range, Action<PlayerEntity> callback, bool includeSelf = true)
            => ForEachOnSameMap(origin, callback, includeSelf);
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

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly Dictionary<int, MapSessionData> _byEid = new();
        public void Register(EntityId id, MapSessionData s) => _byEid[id.Value] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId.Value);
    }
}
