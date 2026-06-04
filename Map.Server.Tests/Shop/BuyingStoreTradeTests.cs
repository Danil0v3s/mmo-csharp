using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers.Shop;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Shop.Buying;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// GP-BUYSTORE — click + sell-in: CZ_REQ_CLICK_TO_BUYING_STORE → visitor list; CZ_REQ_TRADE_BUYING_STORE
/// → buyingstore_trade (item → buyer, escrow → seller) with the result/update/delete emits.
/// </summary>
public class BuyingStoreTradeTests
{
    private const int Potion = 501;

    [Fact]
    public void Click_sends_the_visitor_the_offer_list()
    {
        var ctx = Build();
        ctx.Open(zenyLimit: 50000, (Potion, 10, 1000));

        ctx.Svc.VisitorListReq(ctx.Seller, ctx.Buyer.AccountId);

        var list = ctx.Outbound(ctx.SellerSession).Single(x => Header(x) == (ushort)PacketHeader.ZC_ACK_ITEMLIST_BUYING_STORE);
        Assert.Equal((uint)ctx.Buyer.AccountId, BitConverter.ToUInt32(list, 4)); // AID
        Assert.Equal((uint)ctx.StoreId(), BitConverter.ToUInt32(list, 8));        // store id
        Assert.Equal(50000, BitConverter.ToInt32(list, 12));                      // escrow limit
        // offer at offset 16: price.L amount.W type.B nameId.W
        Assert.Equal(1000, BitConverter.ToInt32(list, 16));
        Assert.Equal(10, BitConverter.ToInt16(list, 20));
    }

    [Fact]
    public void Selling_in_transfers_item_and_pays_seller_from_escrow_and_emits()
    {
        var ctx = Build(sellerHas: 5);
        ctx.Open(zenyLimit: 50000, (Potion, 10, 1000));

        var ok = ctx.Svc.Trade(ctx.Seller, ctx.Buyer.AccountId, ctx.StoreId(),
            new (short index, short amount)[] { (0, 3) }); // seller server slot 0, sell 3

        Assert.True(ok);
        Assert.Equal(ctx.SellerStartZeny + 3000u, ctx.SellerSession.CharacterData!.Zeny); // +3000 from escrow
        Assert.Equal(2u, ctx.SellerSession.Inventory!.First(i => i.NameId == Potion).Amount); // 5-3 left
        Assert.Equal(3u, ctx.BuyerSession.Inventory!.First(i => i.NameId == Potion).Amount); // buyer got 3
        // seller delete + buyer store-update + buyer pickup.
        Assert.Contains(ctx.Outbound(ctx.SellerSession), x => Header(x) == (ushort)PacketHeader.ZC_ITEM_DELETE_BUYING_STORE);
        Assert.Contains(ctx.Outbound(ctx.BuyerSession), x => Header(x) == (ushort)PacketHeader.ZC_UPDATE_ITEM_FROM_BUYING_STORE);
        Assert.Contains(ctx.Outbound(ctx.BuyerSession), x => Header(x) == (ushort)PacketHeader.ZC_ITEM_PICKUP_ACK);
    }

    [Fact]
    public void Selling_in_with_stale_store_id_fails()
    {
        var ctx = Build(sellerHas: 5);
        ctx.Open(zenyLimit: 50000, (Potion, 10, 1000));

        var ok = ctx.Svc.Trade(ctx.Seller, ctx.Buyer.AccountId, storeId: 99999,
            new (short, short)[] { (0, 1) });

        Assert.False(ok);
        var fail = ctx.Outbound(ctx.SellerSession).Single(x => Header(x) == (ushort)PacketHeader.ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER);
        Assert.Equal((ushort)BuyStoreSellResult.DealFailed, BitConverter.ToUInt16(fail, 2));
    }

    [Fact]
    public void Selling_an_unwanted_item_emits_overcount()
    {
        var ctx = Build(sellerHas: 5, sellerItem: 909);
        ctx.Open(zenyLimit: 50000, (Potion, 10, 1000)); // wants 501, seller offers 909

        var ok = ctx.Svc.Trade(ctx.Seller, ctx.Buyer.AccountId, ctx.StoreId(), new (short, short)[] { (0, 1) });

        Assert.False(ok);
        var fail = ctx.Outbound(ctx.SellerSession).Single(x => Header(x) == (ushort)PacketHeader.ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER);
        Assert.Equal((ushort)BuyStoreSellResult.OverCount, BitConverter.ToUInt16(fail, 2));
    }

    [Fact]
    public async Task TradeBuyingStoreHandler_converts_index_and_sells()
    {
        var ctx = Build(sellerHas: 5);
        ctx.Open(zenyLimit: 50000, (Potion, 10, 1000));
        var handler = new TradeBuyingStoreHandler(ctx.Registry, ctx.Svc, NullLogger<TradeBuyingStoreHandler>.Instance);

        var p = new CZ_REQ_TRADE_BUYING_STORE();
        typeof(CZ_REQ_TRADE_BUYING_STORE).GetProperty("BuyerAccountId")!.SetValue(p, ctx.Buyer.AccountId);
        typeof(CZ_REQ_TRADE_BUYING_STORE).GetProperty("StoreId")!.SetValue(p, ctx.StoreId());
        typeof(CZ_REQ_TRADE_BUYING_STORE).GetProperty("Lines")!.SetValue(p,
            (IReadOnlyList<BuyStoreSellLine>)new[] { new BuyStoreSellLine(2, (short)Potion, 2) }); // client index 2 → server 0, sell 2
        await handler.HandleAsync(ctx.SellerSession, p);

        Assert.Equal(ctx.SellerStartZeny + 2000u, ctx.SellerSession.CharacterData!.Zeny);
    }

    // --- harness ---

    private static Ctx Build(int sellerHas = 0, int sellerItem = Potion) => new(sellerHas, sellerItem);

    private sealed class Ctx
    {
        public readonly EntityRegistry Registry;
        public readonly BuyingStoreService Svc;
        public readonly PlayerEntity Buyer, Seller;
        public readonly MapSessionData BuyerSession, SellerSession;
        public readonly uint SellerStartZeny = 100;

        public Ctx(int sellerHas, int sellerItem)
        {
            var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
            Registry = new EntityRegistry(new StubWorld(map));
            Buyer = Add(2, 2002, "Buyer");
            Seller = Add(1, 1001, "Seller");
            BuyerSession = Sess(Buyer, zeny: 60000);
            BuyerSession.Inventory = new List<InventoryItem>();
            SellerSession = Sess(Seller, zeny: SellerStartZeny);
            SellerSession.Inventory = sellerHas > 0
                ? new List<InventoryItem> { new() { Id = 1, ServerIndex = 0, NameId = (uint)sellerItem, Amount = (uint)sellerHas, Identified = true } }
                : new List<InventoryItem>();
            var sessions = new FakeSessions((Buyer.Id, Buyer.AccountId, BuyerSession), (Seller.Id, Seller.AccountId, SellerSession));
            var client = new BuyingStoreClientService(new NoOpVisibility(), sessions, NullLogger<BuyingStoreClientService>.Instance);
            Svc = new BuyingStoreService(NullLogger<BuyingStoreService>.Instance, sessions, client, new FakeItems(), Registry);
        }

        public void Open(long zenyLimit, params (int nameId, short amount, int price)[] offers)
            => Svc.Update(Buyer, "Shop", zenyLimit, offers.Select(o => (o.nameId, o.amount, o.price)).ToList());

        public uint StoreId() => Svc.StoreIdOf(Buyer.Id)!.Value;

        private PlayerEntity Add(int charId, int accId, string name)
        {
            var pc = new PlayerEntity(charId, accId, name, Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
            Registry.Add(pc);
            return pc;
        }

        private static MapSessionData Sess(PlayerEntity pc, uint zeny)
            => new(TestSocketFactory.CreateSocketPair().ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
            { AccountId = pc.AccountId, CharacterId = pc.CharacterId, AuthState = MapAuthState.Spawned, EntityId = pc.Id,
              CharacterData = new Core.Server.IPC.CharacterDataResponse { Zeny = zeny } };

        public IReadOnlyList<byte[]> Outbound(MapSessionData s)
        {
            var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
        }
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public FakeSessions(params (EntityId eid, int acc, MapSessionData s)[] entries)
        { foreach (var (eid, acc, s) in entries) { _byEid[eid] = s; _byAcc[acc] = s; } }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId);
        public MapSessionData? GetByAccountId(int acc) => _byAcc.GetValueOrDefault(acc);
    }

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 1;
        public ItemEntity? Get(uint itemId) => new() { Id = itemId, Type = "Healing" };
        public ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class NoOpVisibility : IVisibilityService
    {
        public void SendToArea(Entity src, Core.Server.Packets.OutgoingPacket packet, SendTarget target = SendTarget.Area) { }
        public void SendToSelf(PlayerEntity player, Core.Server.Packets.OutgoingPacket packet) { }
        public void NotifySpawnedToArea(Entity entered) { }
        public void NotifyVanishedToArea(Entity gone, VanishReason reason) { }
        public void NotifyMoveToArea(Entity walker, short fromX, short fromY, short toX, short toY, uint startTime) { }
        public void SendCurrentViewToSelf(PlayerEntity self) { }
        public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY) { }
        public IReadOnlyList<Entity> NewlyVisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> NewlyInvisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
