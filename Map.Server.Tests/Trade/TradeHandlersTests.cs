using System.Collections.Concurrent;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Trade;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Trade;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Trade;

/// <summary>
/// End-to-end wire-level coverage of the trade flow. Drives the
/// packet handlers, asserts on the packet-id stream queued back to
/// each side, plus the resulting inventory/zeny state.
/// </summary>
public class TradeHandlersTests
{
    [Fact]
    public async Task FullFlow_EmitsRightPacketStream_AndSwapsAtomically()
    {
        var ctx = New();
        var a = ctx.AddPc(charId: 100, accountId: 1000);
        var b = ctx.AddPc(charId: 200, accountId: 2000);
        a.X = 50; a.Y = 50; b.X = 51; b.Y = 50;
        var aSess = ctx.SessionOf(a); var bSess = ctx.SessionOf(b);

        aSess.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Refine = 5 },
        };
        aSess.CharacterData = new CharacterDataResponse { Zeny = 1000 };
        bSess.Inventory = new List<InventoryItem>();
        bSess.CharacterData = new CharacterDataResponse { Zeny = 500 };

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.ReqHandler.HandleAsync(aSess, MakeReq(b.AccountId));
        AssertSent(bSess, (ushort)PacketHeader.ZC_REQ_EXCHANGE_ITEM);
        AssertNotSent(aSess, (ushort)PacketHeader.ZC_ACK_EXCHANGE_ITEM);

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.AckHandler.HandleAsync(bSess, MakeAck(3));
        AssertSent(aSess, (ushort)PacketHeader.ZC_ACK_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_ACK_EXCHANGE_ITEM);

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.AddHandler.HandleAsync(aSess, MakeAdd(index: 2, amount: 3));
        AssertSent(aSess, (ushort)PacketHeader.ZC_ACK_ADD_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_ADD_EXCHANGE_ITEM);
        Assert.Single(aSess.Trade!.Items);

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.AddHandler.HandleAsync(aSess, MakeAdd(index: 0, amount: 200));
        AssertSent(aSess, (ushort)PacketHeader.ZC_ACK_ADD_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_ADD_EXCHANGE_ITEM);
        Assert.Equal(200, aSess.Trade!.Zeny);

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.ConcludeHandler.HandleAsync(aSess, new CZ_CONCLUDE_EXCHANGE_ITEM());
        AssertSent(aSess, (ushort)PacketHeader.ZC_CONCLUDE_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_CONCLUDE_EXCHANGE_ITEM);
        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.ConcludeHandler.HandleAsync(bSess, new CZ_CONCLUDE_EXCHANGE_ITEM());
        AssertSent(aSess, (ushort)PacketHeader.ZC_CONCLUDE_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_CONCLUDE_EXCHANGE_ITEM);

        ClearOutbound(aSess); ClearOutbound(bSess);
        await ctx.ExecHandler.HandleAsync(aSess, new CZ_EXEC_EXCHANGE_ITEM());
        // Waiting on B — no success yet.
        AssertNotSent(aSess, (ushort)PacketHeader.ZC_EXEC_EXCHANGE_ITEM);
        AssertNotSent(bSess, (ushort)PacketHeader.ZC_EXEC_EXCHANGE_ITEM);

        await ctx.ExecHandler.HandleAsync(bSess, new CZ_EXEC_EXCHANGE_ITEM());
        AssertSent(aSess, (ushort)PacketHeader.ZC_EXEC_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_EXEC_EXCHANGE_ITEM);

        Assert.Equal(7u, aSess.Inventory![0].Amount);
        Assert.Contains(bSess.Inventory!, i => i.NameId == 501 && i.Amount == 3);
        Assert.Equal(800u, aSess.CharacterData!.Zeny);
        Assert.Equal(700u, bSess.CharacterData!.Zeny);
        Assert.Null(aSess.Trade);
        Assert.Null(bSess.Trade);
    }

    [Fact]
    public async Task Cancel_ClearsBothSidesAndEmitsCancelPacket()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000);
        var b = ctx.AddPc(200, 2000);
        a.X = 50; a.Y = 50; b.X = 51; b.Y = 50;
        ctx.Service.Request(a, b);
        ctx.Service.Acknowledge(b, accept: true);

        var aSess = ctx.SessionOf(a); var bSess = ctx.SessionOf(b);
        ClearOutbound(aSess); ClearOutbound(bSess);

        await ctx.CancelHandler.HandleAsync(aSess, new CZ_CANCEL_EXCHANGE_ITEM());

        AssertSent(aSess, (ushort)PacketHeader.ZC_CANCEL_EXCHANGE_ITEM);
        AssertSent(bSess, (ushort)PacketHeader.ZC_CANCEL_EXCHANGE_ITEM);
        Assert.Null(aSess.Trade);
        Assert.Null(bSess.Trade);
    }

    [Fact]
    public async Task Request_TargetOffline_AcksTargetNotExist()
    {
        var ctx = New();
        var a = ctx.AddPc(100, 1000);
        var aSess = ctx.SessionOf(a);
        ClearOutbound(aSess);

        await ctx.ReqHandler.HandleAsync(aSess, MakeReq(targetAccountId: 9999));

        var sent = OutboundQueue(aSess).ToList();
        var ack = sent.Single(b => HeaderOf(b) == (ushort)PacketHeader.ZC_ACK_EXCHANGE_ITEM);
        Assert.Equal((byte)1, ack[2]); // result = TargetNotExist
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

    // --- packet construction (reflection sidesteps private setters) ---

    private static CZ_REQ_EXCHANGE_ITEM MakeReq(int targetAccountId)
    {
        var p = new CZ_REQ_EXCHANGE_ITEM();
        typeof(CZ_REQ_EXCHANGE_ITEM).GetProperty("TargetAccountId")!.SetValue(p, targetAccountId);
        return p;
    }

    private static CZ_ACK_EXCHANGE_ITEM MakeAck(byte result)
    {
        var p = new CZ_ACK_EXCHANGE_ITEM();
        typeof(CZ_ACK_EXCHANGE_ITEM).GetProperty("Result")!.SetValue(p, result);
        return p;
    }

    private static CZ_ADD_EXCHANGE_ITEM MakeAdd(ushort index, int amount)
    {
        var p = new CZ_ADD_EXCHANGE_ITEM();
        typeof(CZ_ADD_EXCHANGE_ITEM).GetProperty("Index")!.SetValue(p, index);
        typeof(CZ_ADD_EXCHANGE_ITEM).GetProperty("Amount")!.SetValue(p, amount);
        return p;
    }

    // --- harness ---

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var svc = new TradeService(entities, sessions, NullLogger<TradeService>.Instance);
        return new TestContext(
            svc, entities, sessions,
            new ReqExchangeItemHandler(entities, svc, sessions, NullLogger<ReqExchangeItemHandler>.Instance),
            new AckExchangeItemHandler(entities, svc, sessions, NullLogger<AckExchangeItemHandler>.Instance),
            new AddExchangeItemHandler(entities, svc, sessions, NullLogger<AddExchangeItemHandler>.Instance),
            new ConcludeExchangeItemHandler(entities, svc, sessions),
            new CancelExchangeItemHandler(entities, svc, sessions),
            new ExecExchangeItemHandler(entities, svc, sessions, NullLogger<ExecExchangeItemHandler>.Instance),
            (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        TradeService Service,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        ReqExchangeItemHandler ReqHandler,
        AckExchangeItemHandler AckHandler,
        AddExchangeItemHandler AddHandler,
        ConcludeExchangeItemHandler ConcludeHandler,
        CancelExchangeItemHandler CancelHandler,
        ExecExchangeItemHandler ExecHandler,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId)
        {
            var pc = new PlayerEntity(charId, accountId, $"P{charId}", Guid.NewGuid(), MapId, 100, 100);
            pc.Hp = pc.MaxHp = 1000;
            pc.Level = 99; pc.JobLevel = ExpTable.MaxJobLevel;
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
