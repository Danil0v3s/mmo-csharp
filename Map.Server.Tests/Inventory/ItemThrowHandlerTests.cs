using System.Collections.Concurrent;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Inventory;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory;

/// <summary>
/// Wire-level coverage for <see cref="ItemThrowHandler"/>. Drives the
/// drop packet, asserts on the outbound ack, the inventory delta, and
/// that the floor entity actually spawned.
/// </summary>
public class ItemThrowHandlerTests
{
    [Fact]
    public async Task Drop_DecrementsInventoryAndSpawnsFloorItemAndAcks()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Identified = true },
        };
        ClearOutbound(s);

        await ctx.Handler.HandleAsync(s, MakeThrow(clientIndex: 2, amount: 3));

        AssertSent(s, (ushort)PacketHeader.ZC_ITEM_THROW_ACK);
        Assert.Equal(7u, s.Inventory[0].Amount);
        // Floor item is broadcast/registered through the entity registry.
        Assert.Single(ctx.Entities.All().OfType<FloorItemEntity>());
    }

    [Fact]
    public async Task Drop_EntireStack_RemovesInventorySlot()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { Id = 999, ServerIndex = 0, NameId = 501, Amount = 5, Identified = true },
        };
        ClearOutbound(s);

        await ctx.Handler.HandleAsync(s, MakeThrow(clientIndex: 2, amount: 5));

        AssertSent(s, (ushort)PacketHeader.ZC_ITEM_THROW_ACK);
        Assert.Empty(s.Inventory);
        // Persisted row id should be flagged for deletion.
        Assert.Contains(999, s.RemovedInventoryIds);
    }

    [Fact]
    public async Task Drop_EquippedItem_Rejected()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 1101, Amount = 1, Identified = true, Equip = EquipBits.HandR },
        };
        ClearOutbound(s);

        await ctx.Handler.HandleAsync(s, MakeThrow(clientIndex: 2, amount: 1));

        // No ack on rejection (rAthena fall-through: drop is silently refused).
        Assert.Empty(OutboundQueue(s));
        Assert.Equal(1u, s.Inventory[0].Amount);
        Assert.Empty(ctx.Entities.All().OfType<FloorItemEntity>());
    }

    [Fact]
    public async Task Drop_AmountOverStack_Rejected()
    {
        var ctx = New();
        var pc = ctx.AddPc(100, 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 3, Identified = true },
        };
        ClearOutbound(s);

        await ctx.Handler.HandleAsync(s, MakeThrow(clientIndex: 2, amount: 10));

        Assert.Empty(OutboundQueue(s));
        Assert.Equal(3u, s.Inventory[0].Amount);
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

    private static CZ_ITEM_THROW MakeThrow(ushort clientIndex, ushort amount)
    {
        var p = new CZ_ITEM_THROW();
        typeof(CZ_ITEM_THROW).GetProperty("ClientIndex")!.SetValue(p, clientIndex);
        typeof(CZ_ITEM_THROW).GetProperty("Amount")!.SetValue(p, amount);
        return p;
    }

    // --- harness ---

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var ids = new EntityIdAllocator();
        var drops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var sessions = new InMemorySessions();
        return new TestContext(
            drops, entities, sessions, world,
            new ItemThrowHandler(entities, drops, world,
                new NoMapFlagService(),
                new NoStatusChangeService(),
                NullLogger<ItemThrowHandler>.Instance),
            (uint)mapName.GetHashCode());
    }

    /// <summary>Test stub — reports every mapflag as unset.</summary>
    private sealed class NoMapFlagService : IMapFlagService
    {
        public bool IsSet(string mapName, MapFlag flag) => false;
    }

    /// <summary>Test stub — no active SCs so <c>CanAct</c> always returns true.</summary>
    private sealed class NoStatusChangeService : IStatusChangeService
    {
        public Map.Server.Status.StatusChange? Start(
            Entity target, StatusType type,
            int val1, int val2, int val3, int val4,
            int durationMs,
            Entity? source = null,
            long nowTick = long.MinValue) => null;
        public bool End(Entity target, StatusType type) => false;
        public Map.Server.Status.StatusChange? Get(Entity target, StatusType type) => null;
        public void Tick(long nowTick) { }
        // ST.1 no-op fakes.
        public int ClearAll(Entity target, byte type = 0) => 0;
        public int ClearBuffs(Entity target, SccbFlag flag) => 0;
        public int ClearOnChangeMap(Entity target) => 0;
        public int ClearOnLogout(Entity target) => 0;
        public int Spread(Entity source, Entity target) => 0;
        public int GetMaxStacks(StatusType type) => 1;
        public bool IsDisabledOnMap(uint mapId, StatusType type) => false;
    }

    private sealed record TestContext(
        ItemDropService Drops,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        StubWorldRegistry World,
        ItemThrowHandler Handler,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId)
        {
            var pc = new PlayerEntity(charId, accountId, $"P{charId}", Guid.NewGuid(), MapId, 50, 50);
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
