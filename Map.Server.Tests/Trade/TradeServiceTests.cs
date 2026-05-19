using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Tests.Session;
using Map.Server.Status;
using Map.Server.Trade;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Trade;

public class TradeServiceTests
{
    [Fact]
    public void Request_TooFar_Rejects()
    {
        var ctx = New();
        var a = ctx.AddPc(charId: 1, x: 50, y: 50);
        var b = ctx.AddPc(charId: 2, x: 200, y: 200);
        Assert.Equal(TradeOpResult.TargetTooFar, ctx.Service.Request(a, b));
    }

    [Fact]
    public void FullFlow_Commit_SwapsItemsAndZeny()
    {
        var ctx = New();
        var a = ctx.AddPc(charId: 1, x: 50, y: 50);
        var b = ctx.AddPc(charId: 2, x: 51, y: 50);

        // Pre-load both inventories.
        var aSess = ctx.SessionOf(a);
        var bSess = ctx.SessionOf(b);
        aSess.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10 }, // Red Potion
        };
        aSess.CharacterData = new CharacterDataResponse { Zeny = 1000 };
        bSess.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 502, Amount = 5 }, // Orange Potion
        };
        bSess.CharacterData = new CharacterDataResponse { Zeny = 500 };

        // 1. A requests trade with B
        Assert.Equal(TradeOpResult.Ok, ctx.Service.Request(a, b));
        // 2. B accepts
        Assert.Equal(TradeOpResult.Ok, ctx.Service.Acknowledge(b, accept: true));
        // 3. A offers 3 Red Potion + 200 zeny; B offers 2 Orange Potion + 100 zeny
        Assert.Equal(TradeOpResult.Ok, ctx.Service.AddItem(a, inventoryIndex: 0, amount: 3));
        Assert.Equal(TradeOpResult.Ok, ctx.Service.AddZeny(a, 200));
        Assert.Equal(TradeOpResult.Ok, ctx.Service.AddItem(b, inventoryIndex: 0, amount: 2));
        Assert.Equal(TradeOpResult.Ok, ctx.Service.AddZeny(b, 100));
        // 4. Both press OK then Trade
        Assert.Equal(TradeOpResult.Ok, ctx.Service.Ok(a));
        Assert.Equal(TradeOpResult.Ok, ctx.Service.Ok(b));
        Assert.Equal(TradeOpResult.InvalidStage, ctx.Service.Commit(a)); // waiting for B
        Assert.Equal(TradeOpResult.Ok, ctx.Service.Commit(b)); // closes the deal

        // A: lost 3 Red Potion, gained 2 Orange Potion + 100 zeny - 200 zeny.
        Assert.Equal(7u, aSess.Inventory[0].Amount); // 10 - 3
        Assert.Contains(aSess.Inventory, i => i.NameId == 502 && i.Amount == 2);
        Assert.Equal(900u, aSess.CharacterData.Zeny); // 1000 - 200 + 100

        // B: lost 2 Orange Potion, gained 3 Red Potion + 200 - 100 zeny.
        Assert.Equal(3u, bSess.Inventory[0].Amount); // 5 - 2
        Assert.Contains(bSess.Inventory, i => i.NameId == 501 && i.Amount == 3);
        Assert.Equal(600u, bSess.CharacterData.Zeny); // 500 + 200 - 100

        // Both sides' trade state cleared.
        Assert.Null(aSess.Trade);
        Assert.Null(bSess.Trade);
    }

    [Fact]
    public void Cancel_ClearsBothSides()
    {
        var ctx = New();
        var a = ctx.AddPc(1, 50, 50);
        var b = ctx.AddPc(2, 51, 50);
        ctx.Service.Request(a, b);
        ctx.Service.Acknowledge(b, accept: true);

        Assert.Equal(TradeOpResult.Ok, ctx.Service.Cancel(a));
        Assert.Null(ctx.SessionOf(a).Trade);
        Assert.Null(ctx.SessionOf(b).Trade);
    }

    [Fact]
    public void AddItem_BeforeAccept_Rejected()
    {
        var ctx = New();
        var a = ctx.AddPc(1, 50, 50);
        var b = ctx.AddPc(2, 51, 50);
        ctx.SessionOf(a).Inventory = new List<InventoryItem> { new() { NameId = 501, Amount = 5 } };

        ctx.Service.Request(a, b);
        // B hasn't acknowledged — neither side can add items yet.
        Assert.Equal(TradeOpResult.InvalidStage, ctx.Service.AddItem(a, 0, 1));
    }

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var svc = new TradeService(entities, sessions, NullLogger<TradeService>.Instance);
        return new TestContext(svc, entities, sessions, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        TradeService Service,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);

            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide,
                heartbeatTimeout: 30000,
                packetFactory: new PacketSystem().Factory,
                sizeRegistry: new PacketSystem().Registry,
                logger: NullLogger.Instance)
            {
                AccountId = charId,
                CharacterId = charId,
                AuthState = MapAuthState.Spawned,
                EntityId = pc.Id,
            };
            Sessions.Register(pc.Id, session);
            return pc;
        }
        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _bySid = new();
        public void Register(EntityId id, MapSessionData s) => _bySid[id] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _bySid.GetValueOrDefault(entityId);
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
