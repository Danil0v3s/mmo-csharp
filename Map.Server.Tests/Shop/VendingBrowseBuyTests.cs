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
using Map.Server.Shop.Vending;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// GP-VEND — browse + buy: CZ_REQ_VENDING_ITEMS → vending_vendinglistreq → price list; the buyer's
/// purchase validates against the stamped vended_id and transfers zeny/items with result/report emits.
/// </summary>
public class VendingBrowseBuyTests
{
    private const uint Potion = 501;

    [Fact]
    public void VendingListReq_stamps_vended_id_and_sends_price_list()
    {
        var ctx = Build();
        ctx.Open(("MyShop", 0, qty: 10, price: 100));

        ctx.Svc.VendingListReq(ctx.Buyer, ctx.Vendor.AccountId);

        var list = ctx.Outbound(ctx.BuyerSession).OfType<byte[]>()
            .Single(x => Header(x) == (ushort)PacketHeader.ZC_PC_PURCHASE_ITEMLIST_FROMMC);
        Assert.Equal((uint)ctx.Vendor.AccountId, BitConverter.ToUInt32(list, 4)); // owner AID
        // entry at offset 8: price.L amount.W index.W ...
        Assert.Equal(100, BitConverter.ToInt32(list, 8));
        Assert.Equal(10, BitConverter.ToInt16(list, 12));
        Assert.Equal(2, BitConverter.ToInt16(list, 14));   // client index = server 0 + 2
        Assert.NotEqual(0L, ctx.Buyer.VendedId);            // anti-desync id stamped
    }

    [Fact]
    public void Purchase_transfers_zeny_and_items_and_emits_feedback()
    {
        var ctx = Build(buyerZeny: 1000);
        ctx.Open(("Shop", 0, qty: 10, price: 100));
        ctx.Svc.VendingListReq(ctx.Buyer, ctx.Vendor.AccountId); // stamps VendedId

        var ok = ctx.Svc.PurchaseReq(ctx.Buyer, ctx.Vendor.AccountId, ctx.Buyer.VendedId,
            new (short index, short qty)[] { (0, 3) }); // server index 0, qty 3

        Assert.True(ok);
        Assert.Equal(700u, ctx.BuyerSession.CharacterData!.Zeny);   // -300
        Assert.Equal(500u + 300u, ctx.VendorSession.CharacterData!.Zeny); // +300 (no tax)
        Assert.Equal(3u, ctx.BuyerSession.Inventory!.First(i => i.NameId == Potion).Amount); // bought 3
        Assert.Equal(7u, ctx.VendorSession.Cart!.First(i => i.NameId == Potion).Amount);     // cart 10-3
        // buyer pickup + zeny par-change; vendor sale report + zeny par-change.
        Assert.Contains(ctx.Outbound(ctx.BuyerSession), x => Header(x) == (ushort)PacketHeader.ZC_ITEM_PICKUP_ACK);
        Assert.Contains(ctx.Outbound(ctx.VendorSession), x => Header(x) == (ushort)PacketHeader.ZC_DELETEITEM_FROM_MCSTORE);
    }

    [Fact]
    public void Purchase_with_stale_vended_id_is_rejected_with_store_incorrect()
    {
        var ctx = Build(buyerZeny: 1000);
        ctx.Open(("Shop", 0, qty: 10, price: 100));

        var ok = ctx.Svc.PurchaseReq(ctx.Buyer, ctx.Vendor.AccountId, venderId: 99999,
            new (short, short)[] { (0, 1) });

        Assert.False(ok);
        var res = ctx.Outbound(ctx.BuyerSession).Single(x => Header(x) == (ushort)PacketHeader.ZC_PC_PURCHASE_RESULT_FROMMC);
        Assert.Equal((byte)VendPurchaseResult.StoreIncorrect, res[6]);
    }

    [Fact]
    public void Purchase_without_enough_zeny_emits_no_zeny()
    {
        var ctx = Build(buyerZeny: 100);
        ctx.Open(("Shop", 0, qty: 10, price: 100));
        ctx.Svc.VendingListReq(ctx.Buyer, ctx.Vendor.AccountId);

        var ok = ctx.Svc.PurchaseReq(ctx.Buyer, ctx.Vendor.AccountId, ctx.Buyer.VendedId,
            new (short, short)[] { (0, 5) }); // 500z > 100z

        Assert.False(ok);
        var res = ctx.Outbound(ctx.BuyerSession).Single(x => Header(x) == (ushort)PacketHeader.ZC_PC_PURCHASE_RESULT_FROMMC);
        Assert.Equal((byte)VendPurchaseResult.NoZeny, res[6]);
        Assert.Equal(100u, ctx.BuyerSession.CharacterData!.Zeny); // unchanged — no partial transfer
    }

    [Fact]
    public async Task PurchaseFromMcHandler_converts_index_and_buys()
    {
        var ctx = Build(buyerZeny: 1000);
        ctx.Open(("Shop", 0, qty: 10, price: 100));
        ctx.Svc.VendingListReq(ctx.Buyer, ctx.Vendor.AccountId);
        var handler = new PurchaseFromMcHandler(ctx.Registry, ctx.Svc, NullLogger<PurchaseFromMcHandler>.Instance);

        var p = new CZ_PC_PURCHASE_ITEMLIST_FROMMC();
        typeof(CZ_PC_PURCHASE_ITEMLIST_FROMMC).GetProperty("VendorAccountId")!.SetValue(p, ctx.Vendor.AccountId);
        typeof(CZ_PC_PURCHASE_ITEMLIST_FROMMC).GetProperty("Lines")!.SetValue(p,
            (IReadOnlyList<VendBuyLine>)new[] { new VendBuyLine(2, 2) }); // amount 2, client index 2 → server 0
        await handler.HandleAsync(ctx.BuyerSession, p);

        Assert.Equal(800u, ctx.BuyerSession.CharacterData!.Zeny); // -200
    }

    // --- harness ---

    private static Ctx Build(int buyerZeny = 500) => new(buyerZeny);

    private sealed class Ctx
    {
        public readonly EntityRegistry Registry;
        public readonly VendingService Svc;
        public readonly PlayerEntity Vendor, Buyer;
        public readonly MapSessionData VendorSession, BuyerSession;

        public Ctx(int buyerZeny)
        {
            var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
            Registry = new EntityRegistry(new StubWorld(map));
            Vendor = Add(2, 2002, "Vendor");
            Buyer = Add(1, 1001, "Buyer");
            VendorSession = Sess(Vendor, zeny: 500);
            BuyerSession = Sess(Buyer, zeny: (uint)buyerZeny);
            VendorSession.Cart = new List<InventoryItem> { new() { Id = 1, ServerIndex = 0, NameId = Potion, Amount = 10, Identified = true } };
            BuyerSession.Inventory = new List<InventoryItem>();
            var sessions = new FakeSessions((Vendor.Id, Vendor.AccountId, VendorSession), (Buyer.Id, Buyer.AccountId, BuyerSession));
            var client = new VendingClientService(new NoOpVisibility(), sessions, NullLogger<VendingClientService>.Instance);
            Svc = new VendingService(NullLogger<VendingService>.Instance, sessions, client, new FakeItems(), Registry);
        }

        public void Open(params (string title, int serverIdx, short qty, int price)[] offers)
            => Svc.Update(Vendor, offers[0].title, offers.Select(o => ((short)o.serverIdx, o.qty, o.price)).ToList());

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
