using System.Collections.Concurrent;
using Core.Server.Packets;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

/// <summary>
/// Wave 92 — coverage for the wire-emit layer of party lifecycle
/// packets (<see cref="PartyClientService"/>). Asserts on the outbound
/// packet ids queued to the founder / target / member sessions and on
/// the cached <see cref="MapPartyEntity"/> mutations that follow the
/// rAthena <c>party_optionchanged</c> / <c>party_member_withdraw</c>
/// in-memory edits.
/// </summary>
public class PartyClientServiceTests
{
    [Fact]
    public void NotifyPartyCreated_EnqueuesAckAndMemberRow()
    {
        var ctx = New();
        var founder = ctx.AddPc(100, 1000, "Alice");
        var founderSess = ctx.SessionOf(founder);
        ClearOutbound(founderSess);

        ctx.Service.NotifyPartyCreated(founder, partyId: 42, partyName: "alpha");

        var ids = OutboundQueue(founderSess).Select(HeaderOf).ToArray();
        Assert.Contains((ushort)PacketHeader.ZC_ACK_MAKE_GROUP, ids);
        Assert.Contains((ushort)PacketHeader.ZC_ADD_MEMBER_TO_GROUP, ids);
    }

    [Fact]
    public void NotifyJoinRequest_EnqueuesPopupAndStashesInvite()
    {
        var ctx = New();
        var inviter = ctx.AddPc(100, 1000, "Alice");
        var target = ctx.AddPc(200, 2000, "Bob");
        var targetSess = ctx.SessionOf(target);
        ClearOutbound(targetSess);

        ctx.Service.NotifyJoinRequest(target, inviter, partyId: 42, partyName: "alpha");

        AssertSent(targetSess, (ushort)PacketHeader.ZC_PARTY_JOIN_REQ);
        var pending = ctx.Service.ConsumePendingInvite(target.CharacterId);
        Assert.NotNull(pending);
        Assert.Equal(42, pending!.Value.partyId);
        Assert.Equal(inviter.CharacterId, pending.Value.inviterCharacterId);
    }

    [Fact]
    public void ConsumePendingInvite_OnlyOnce_ThenReturnsNull()
    {
        var ctx = New();
        ctx.Service.StashPendingInvite(targetCharacterId: 7, partyId: 11, inviterCharacterId: 9);

        Assert.NotNull(ctx.Service.ConsumePendingInvite(7));
        Assert.Null(ctx.Service.ConsumePendingInvite(7));
    }

    [Fact]
    public void NotifyInviteReply_EnqueuesAckOnInviter()
    {
        var ctx = New();
        var inviter = ctx.AddPc(100, 1000, "Alice");
        var inviterSess = ctx.SessionOf(inviter);
        ClearOutbound(inviterSess);

        ctx.Service.NotifyInviteReply(inviter, "Bob", result: 1);

        AssertSent(inviterSess, (ushort)PacketHeader.ZC_PARTY_JOIN_REQ_ACK);
    }

    [Fact]
    public void NotifyMemberJoined_FansOutToLocalPartyMembers()
    {
        var ctx = New();
        var alice = ctx.AddPc(100, 1000, "Alice");
        var bob = ctx.AddPc(200, 2000, "Bob");
        var carol = ctx.AddPc(300, 3000, "Carol");
        alice.PartyId = bob.PartyId = 42;
        carol.PartyId = 99;
        var aliceSess = ctx.SessionOf(alice);
        var bobSess = ctx.SessionOf(bob);
        var carolSess = ctx.SessionOf(carol);
        ClearOutbound(aliceSess); ClearOutbound(bobSess); ClearOutbound(carolSess);

        var party = new MapPartyEntity(42) { Name = "alpha", Exp = 1, Item = 1 };
        var newMember = new MapPartyMember
        {
            CharacterId = 200, AccountId = 2000, Name = "Bob",
            ClassId = 23, Level = 5, Online = true, MapName = "test_map",
        };
        ctx.Service.NotifyMemberJoined(party, newMember);

        AssertSent(aliceSess, (ushort)PacketHeader.ZC_ADD_MEMBER_TO_GROUP);
        AssertSent(bobSess, (ushort)PacketHeader.ZC_ADD_MEMBER_TO_GROUP);
        AssertNotSent(carolSess, (ushort)PacketHeader.ZC_ADD_MEMBER_TO_GROUP);
    }

    [Fact]
    public void NotifyMemberWithdraw_FansOutAndRemovesFromCache()
    {
        var ctx = New();
        var alice = ctx.AddPc(100, 1000, "Alice");
        var bob = ctx.AddPc(200, 2000, "Bob");
        alice.PartyId = bob.PartyId = 42;
        var aliceSess = ctx.SessionOf(alice);
        var bobSess = ctx.SessionOf(bob);
        ClearOutbound(aliceSess); ClearOutbound(bobSess);

        var party = new MapPartyEntity(42) { Name = "alpha" };
        party.Members[100] = new MapPartyMember { CharacterId = 100, AccountId = 1000, Name = "Alice", Online = true };
        party.Members[200] = new MapPartyMember { CharacterId = 200, AccountId = 2000, Name = "Bob", Online = true };

        ctx.Service.NotifyMemberWithdraw(party, accountId: 2000, memberName: "Bob", reason: 0);

        AssertSent(aliceSess, (ushort)PacketHeader.ZC_DELETE_MEMBER_FROM_GROUP);
        AssertSent(bobSess, (ushort)PacketHeader.ZC_DELETE_MEMBER_FROM_GROUP);
        Assert.False(party.Members.ContainsKey(200), "Withdrawing member should be evicted from MapPartyEntity.Members");
        Assert.True(party.Members.ContainsKey(100), "Remaining members should be preserved");
    }

    [Fact]
    public void NotifyOptionChanged_FansOutAndMutatesCache()
    {
        var ctx = New();
        var alice = ctx.AddPc(100, 1000, "Alice");
        alice.PartyId = 42;
        var aliceSess = ctx.SessionOf(alice);
        ClearOutbound(aliceSess);

        var party = new MapPartyEntity(42) { Name = "alpha", Exp = 0, Item = 0 };

        ctx.Service.NotifyOptionChanged(party, expOption: 1, itemPickupRule: 1, itemShareRule: 0);

        AssertSent(aliceSess, (ushort)PacketHeader.ZC_GROUPINFO_CHANGE);
        Assert.Equal((byte)1, party.Exp);
        Assert.Equal((byte)1, party.Item & 1);
        Assert.Equal((byte)0, party.Item & 2);
    }

    [Fact]
    public void NotifyDotRemove_BroadcastsXyMinusOne_ExceptToSelf()
    {
        var ctx = New();
        var alice = ctx.AddPc(100, 1000, "Alice"); // leaving member
        var bob = ctx.AddPc(200, 2000, "Bob");
        alice.PartyId = bob.PartyId = 42;
        var aliceSess = ctx.SessionOf(alice);
        var bobSess = ctx.SessionOf(bob);
        ClearOutbound(aliceSess); ClearOutbound(bobSess);

        var party = new MapPartyEntity(42) { Name = "alpha" };
        party.Members[100] = new MapPartyMember { CharacterId = 100, AccountId = 1000, Name = "Alice" };
        party.Members[200] = new MapPartyMember { CharacterId = 200, AccountId = 2000, Name = "Bob" };

        ctx.Service.NotifyDotRemove(party, characterId: 100, accountId: 1000);

        // rAthena clif_party_xy_remove uses PARTY_SAMEMAP_WOS — exclude self.
        AssertNotSent(aliceSess, (ushort)PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM);
        AssertSent(bobSess, (ushort)PacketHeader.ZC_NOTIFY_POSITION_TO_GROUPM);
    }

    // --- wire helpers ---

    private static ushort HeaderOf(byte[] packet)
        => (ushort)(packet[0] | (packet[1] << 8));

    private static IReadOnlyList<byte[]> OutboundQueue(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) return q.ToArray();
        return Array.Empty<byte[]>();
    }

    private static void ClearOutbound(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) q.Clear();
    }

    private static void AssertSent(MapSessionData session, ushort packetId)
    {
        var ids = OutboundQueue(session).Select(HeaderOf).ToArray();
        Assert.Contains(packetId, ids);
    }

    private static void AssertNotSent(MapSessionData session, ushort packetId)
    {
        var ids = OutboundQueue(session).Select(HeaderOf).ToArray();
        Assert.DoesNotContain(packetId, ids);
    }

    // --- harness ---

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var svc = new PartyClientService(entities, sessions, NullLogger<PartyClientService>.Instance);
        return new TestContext(svc, entities, sessions, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        PartyClientService Service,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId, string name)
        {
            var pc = new PlayerEntity(charId, accountId, name, Guid.NewGuid(), MapId, 50, 50);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);

            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry,
                NullLogger.Instance)
            {
                AccountId = accountId,
                CharacterId = charId,
                AuthState = MapAuthState.Spawned,
                EntityId = pc.Id,
            };
            Sessions.Register(pc.Id, accountId, session);
            return pc;
        }

        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int accountId, MapSessionData s)
        {
            _byEid[id] = s;
            _byAcc[accountId] = s;
        }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
