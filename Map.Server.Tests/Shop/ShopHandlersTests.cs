using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Shop;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Scripting.Records;
using Map.Server.Session;
using Map.Server.Shop;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

public class ShopHandlersTests
{
    [Fact]
    public async Task DealType_Buy_SendsCatalog()
    {
        var ctx = New();
        var npc = ctx.AddShopNpc("Generic Merchant", items: new[]
        {
            new ShopItem(501, 50),
            new ShopItem(502, 200),
        });
        var buyer = ctx.AddPc(1, 1000);
        var bs = ctx.SessionOf(buyer);
        ClearOutbound(bs);

        // Sanity: the NPC + shop wired correctly.
        var stored = ctx.Entities.Get(npc.Id) as NpcEntity;
        Assert.NotNull(stored);
        Assert.NotNull(stored!.Shop);

        await ctx.SelectHandler.HandleAsync(bs, MakeSelect(npc.Id.Value, type: 0));

        AssertSent(bs, (ushort)PacketHeader.ZC_PC_PURCHASE_ITEMLIST);
        Assert.Equal(npc.Id.Value, bs.OpenShopNpcId);
    }

    [Fact]
    public async Task PurchaseItemList_DeductsZenyAndEmitsSuccess()
    {
        var ctx = New();
        var npc = ctx.AddShopNpc("Merchant", items: new[]
        {
            new ShopItem(501, 50),
        });
        var buyer = ctx.AddPc(1, zeny: 500);
        var bs = ctx.SessionOf(buyer);
        bs.OpenShopNpcId = npc.Id.Value;

        await ctx.PurchaseHandler.HandleAsync(bs, MakePurchase(new[] { (501, 5) }));

        // Zeny deducted, item added, ZC_PC_PURCHASE_RESULT(0) sent.
        Assert.Equal(250u, bs.CharacterData!.Zeny);
        Assert.NotNull(bs.Inventory);
        Assert.Contains(bs.Inventory!, i => i.NameId == 501 && i.Amount == 5);
        AssertSentWithResult(bs, (ushort)PacketHeader.ZC_PC_PURCHASE_RESULT, expectedResult: 0);
        Assert.Null(bs.OpenShopNpcId);
    }

    [Fact]
    public async Task PurchaseItemList_NotEnoughZeny_AcksFailure()
    {
        var ctx = New();
        var npc = ctx.AddShopNpc("Merchant", items: new[]
        {
            new ShopItem(501, 1000),
        });
        var buyer = ctx.AddPc(1, zeny: 100);
        var bs = ctx.SessionOf(buyer);
        bs.OpenShopNpcId = npc.Id.Value;

        await ctx.PurchaseHandler.HandleAsync(bs, MakePurchase(new[] { (501, 1) }));

        AssertSentWithResult(bs, (ushort)PacketHeader.ZC_PC_PURCHASE_RESULT, expectedResult: 1);
        // Inventory + zeny untouched on failure.
        Assert.Equal(100u, bs.CharacterData!.Zeny);
    }

    [Fact]
    public async Task SellItemList_AwardsZenyAtHalfPrice()
    {
        var ctx = New();
        ctx.Catalog.Set(501, new ItemEntity { Id = 501, NameAegis = "Red_Potion", PriceBuy = 100 });
        var npc = ctx.AddShopNpc("Merchant", items: Array.Empty<ShopItem>());
        var seller = ctx.AddPc(1, zeny: 0);
        var ss = ctx.SessionOf(seller);
        ss.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Identified = true },
        };
        ss.OpenShopNpcId = npc.Id.Value;

        // Sell 4 of slot 0 (client_index = 2).
        await ctx.SellHandler.HandleAsync(ss, MakeSell(new[] { (clientIndex: (ushort)2, amount: (ushort)4) }));

        // 4 * 50 = 200 zeny gained.
        Assert.Equal(200u, ss.CharacterData!.Zeny);
        Assert.Equal(6u, ss.Inventory[0].Amount);
        AssertSentWithResult(ss, (ushort)PacketHeader.ZC_PC_SELL_RESULT, expectedResult: 0);
    }

    // --- helpers ---

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

    private static void AssertSentWithResult(MapSessionData session, ushort packetId, byte expectedResult)
    {
        var packet = OutboundQueue(session).Single(b => HeaderOf(b) == packetId);
        Assert.Equal(expectedResult, packet[2]);
    }

    private static CZ_ACK_SELECT_DEALTYPE MakeSelect(int npcId, byte type)
    {
        var p = new CZ_ACK_SELECT_DEALTYPE();
        typeof(CZ_ACK_SELECT_DEALTYPE).GetProperty("NpcId")!.SetValue(p, npcId);
        typeof(CZ_ACK_SELECT_DEALTYPE).GetProperty("DealType")!.SetValue(p, type);
        return p;
    }

    private static CZ_PC_PURCHASE_ITEMLIST MakePurchase((int itemId, int amount)[] entries)
    {
        var p = new CZ_PC_PURCHASE_ITEMLIST();
        var list = entries.Select(e => new CZ_PC_PURCHASE_ITEMLIST.BuyEntry(
            (ushort)e.amount, (ushort)e.itemId)).ToList();
        typeof(CZ_PC_PURCHASE_ITEMLIST).GetProperty("Items")!.SetValue(p, list);
        return p;
    }

    private static CZ_PC_SELL_ITEMLIST MakeSell((ushort clientIndex, ushort amount)[] entries)
    {
        var p = new CZ_PC_SELL_ITEMLIST();
        var list = entries.Select(e => new CZ_PC_SELL_ITEMLIST.SellEntry(e.clientIndex, e.amount)).ToList();
        typeof(CZ_PC_SELL_ITEMLIST).GetProperty("Items")!.SetValue(p, list);
        return p;
    }

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var catalog = new StubCatalog();
        var sessions = new InMemorySessions();
        var svc = new ShopService(catalog, sessions, NullLogger<ShopService>.Instance);
        var ids = new EntityIdAllocator();
        return new TestContext(
            svc, catalog, entities, sessions, ids,
            new SelectDealTypeHandler(entities, catalog, NullLogger<SelectDealTypeHandler>.Instance),
            new PurchaseItemListHandler(entities, svc, NullLogger<PurchaseItemListHandler>.Instance),
            new SellItemListHandler(entities, svc, NullLogger<SellItemListHandler>.Instance),
            (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        ShopService Service,
        StubCatalog Catalog,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        EntityIdAllocator Ids,
        SelectDealTypeHandler SelectHandler,
        PurchaseItemListHandler PurchaseHandler,
        SellItemListHandler SellHandler,
        uint MapId)
    {
        public NpcEntity AddShopNpc(string name, IReadOnlyList<ShopItem> items)
        {
            var npc = new NpcEntity(
                Ids.NextNpc(), name, spriteId: 100,
                MapId, 50, 50, dir: 0, triggerArea: null,
                hooks: Map.Server.Scripting.Records.NpcHooks.Empty)
            {
                Shop = new ShopRegistration
                {
                    Kind = ShopKind.Shop, Map = "test", X = 50, Y = 50,
                    Sprite = 100, Name = name, Items = items,
                },
            };
            Entities.Add(npc);
            return npc;
        }

        public PlayerEntity AddPc(int charId, uint zeny)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, 51, 50);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);

            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry,
                NullLogger.Instance)
            {
                AccountId = charId,
                CharacterId = charId,
                AuthState = MapAuthState.Spawned,
                EntityId = pc.Id,
                CharacterData = new CharacterDataResponse { Zeny = zeny },
            };
            Sessions.Register(pc.Id, charId, session);
            return pc;
        }

        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class StubCatalog : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _byId = new();
        public void Set(uint id, ItemEntity row) => _byId[id] = row;
        public int Count => _byId.Count;
        public ItemEntity? Get(uint id) => _byId.GetValueOrDefault(id);
        public ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<ItemEntity> All() => _byId.Values;
        public void Reload() { }
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int accountId, MapSessionData s) { _byEid[id] = s; _byAcc[accountId] = s; }
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
