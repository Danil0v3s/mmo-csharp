using Core.Database.Entities;
using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server.Entities;
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

public class ShopServiceTests
{
    [Fact]
    public void Buy_NotEnoughZeny_Rejected()
    {
        var ctx = New();
        var buyer = ctx.AddPc(1, zeny: 50);
        var shop = StubShop("seller", items: new[] { new ShopItem(501, 100) });
        var result = ctx.Service.Buy(buyer, shop, new[] { (501, 1) });
        Assert.Equal(ShopOpResult.NotEnoughZeny, result);
        // No zeny deducted on failure.
        Assert.Equal(50u, ctx.SessionOf(buyer).CharacterData!.Zeny);
    }

    [Fact]
    public void Buy_UnknownItem_Rejected()
    {
        var ctx = New();
        var buyer = ctx.AddPc(1, zeny: 10_000);
        var shop = StubShop("seller", items: new[] { new ShopItem(501, 50) });
        Assert.Equal(ShopOpResult.InvalidSlot,
            ctx.Service.Buy(buyer, shop, new[] { (999, 1) }));
    }

    [Fact]
    public void Buy_DeductsZenyAndAddsItem()
    {
        var ctx = New();
        var buyer = ctx.AddPc(1, zeny: 1000);
        var shop = StubShop("seller", items: new[]
        {
            new ShopItem(501, 50),
            new ShopItem(502, 200),
        });

        Assert.Equal(ShopOpResult.Ok,
            ctx.Service.Buy(buyer, shop, new[] { (501, 5), (502, 2) }));

        // Cost: 5*50 + 2*200 = 650
        var session = ctx.SessionOf(buyer);
        Assert.Equal(350u, session.CharacterData!.Zeny);
        Assert.NotNull(session.Inventory);
        Assert.Contains(session.Inventory!, i => i.NameId == 501 && i.Amount == 5);
        Assert.Contains(session.Inventory!, i => i.NameId == 502 && i.Amount == 2);
    }

    [Fact]
    public void Sell_AtHalfBuyPrice()
    {
        var ctx = New();
        var seller = ctx.AddPc(1, zeny: 0);
        var session = ctx.SessionOf(seller);
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Identified = true },
        };
        // item_db buy price = 50 → sell at 25 each.
        ctx.Catalog.Set(501, new ItemEntity { Id = 501, NameAegis = "Red_Potion", PriceBuy = 50 });

        Assert.Equal(ShopOpResult.Ok,
            ctx.Service.Sell(seller, new[] { (0, 4) }));

        // 4 * 25 = 100 zeny.
        Assert.Equal(100u, session.CharacterData!.Zeny);
        Assert.Equal(6u, session.Inventory[0].Amount);
    }

    [Fact]
    public void Sell_FullStack_RemovesSlot()
    {
        var ctx = New();
        var seller = ctx.AddPc(1, zeny: 0);
        var session = ctx.SessionOf(seller);
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 3, Id = 42 },
        };
        ctx.Catalog.Set(501, new ItemEntity { Id = 501, NameAegis = "Red_Potion", PriceBuy = 50 });

        ctx.Service.Sell(seller, new[] { (0, 3) });

        Assert.Empty(session.Inventory);
        Assert.Contains(42, session.RemovedInventoryIds);
    }

    private static ShopRegistration StubShop(string name, IReadOnlyList<ShopItem> items)
        => new()
        {
            Kind = ShopKind.Shop, Map = "test", X = 0, Y = 0, Sprite = 0,
            Name = name, Items = items,
        };

    private static TestContext New()
    {
        var catalog = new StubCatalog();
        var sessions = new InMemorySessions();
        var svc = new ShopService(catalog, sessions, NullLogger<ShopService>.Instance);
        return new TestContext(svc, catalog, sessions);
    }

    private sealed record TestContext(
        ShopService Service,
        StubCatalog Catalog,
        InMemorySessions Sessions)
    {
        public PlayerEntity AddPc(int charId, uint zeny)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), 0, 0, 0);
            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry,
                NullLogger.Instance)
            {
                AccountId = charId, CharacterId = charId,
                EntityId = pc.Id,
                AuthState = MapAuthState.Spawned,
                CharacterData = new CharacterDataResponse { Zeny = zeny },
            };
            Sessions.Register(pc.Id, session);
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
        private readonly Dictionary<EntityId, MapSessionData> _bySid = new();
        public void Register(EntityId id, MapSessionData s) => _bySid[id] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _bySid.GetValueOrDefault(entityId);
    }
}
