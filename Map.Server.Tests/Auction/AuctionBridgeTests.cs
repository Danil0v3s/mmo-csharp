using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Handlers.Auction;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Auction;

/// <summary>
/// GP-AUCTION (turn 1) — the auction client packet bridge: CZ handlers (stage/register/cancel/bid/
/// buy-now/search/my-info) → the auction service, and the ZC emits (setitem ack, status message,
/// results list, close ack).
/// </summary>
public class AuctionBridgeTests
{
    private const uint SwordId = 1101;
    private const uint PotionId = 501;

    [Fact]
    public async Task SetItem_stages_an_auctionable_item_and_acks_success()
    {
        var c = Build(Item(0, SwordId, 1));
        await new AuctionSetItemHandler(c.Reg, c.Client, c.Items, NullLogger<AuctionSetItemHandler>.Instance)
            .HandleAsync(c.Session, SetItem(clientIndex: 2)); // server slot 0

        Assert.Equal(0, c.Session.AuctionStageIndex);
        Assert.Equal(1, c.Session.AuctionStageAmount);
        var ack = c.Out(x => Hdr(x) == (ushort)PacketHeader.ZC_ACK_AUCTION_ADD_ITEM).Single();
        Assert.Equal(2, BitConverter.ToInt16(ack, 2)); // client index = server + 2
        Assert.Equal(0, ack[4]);                        // fail = 0
    }

    [Fact]
    public async Task SetItem_rejects_a_consumable()
    {
        var c = Build(Item(0, PotionId, 1)); // Usable → not auctionable
        await new AuctionSetItemHandler(c.Reg, c.Client, c.Items, NullLogger<AuctionSetItemHandler>.Instance)
            .HandleAsync(c.Session, SetItem(clientIndex: 2));

        Assert.Equal(-1, c.Session.AuctionStageIndex);
        var ack = c.Out(x => Hdr(x) == (ushort)PacketHeader.ZC_ACK_AUCTION_ADD_ITEM).Single();
        Assert.Equal(1, ack[4]); // fail = 1
    }

    [Fact]
    public async Task Register_with_staged_item_calls_service_and_confirms()
    {
        var c = Build(Item(0, SwordId, 1));
        c.Session.AuctionStageIndex = 0; c.Session.AuctionStageAmount = 1;
        c.Svc.RegisterResult = 909;

        await new AuctionRegisterHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionRegisterHandler>.Instance)
            .HandleAsync(c.Session, Register(now: 100, max: 1000, hours: 2));

        Assert.Equal((0, 1, 100, 1000, 2), c.Svc.LastRegister);
        Assert.Equal(-1, c.Session.AuctionStageIndex); // stage cleared on success
        Assert.Equal((byte)AuctionResultMessage.BidSuccess, MsgFlag(c));
    }

    [Fact]
    public async Task Register_without_enough_fee_reports_specific_message_and_skips_service()
    {
        var c = Build(zeny: 1000, Item(0, SwordId, 1)); // 2h fee = 24000 > 1000
        c.Session.AuctionStageIndex = 0; c.Session.AuctionStageAmount = 1;

        await new AuctionRegisterHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionRegisterHandler>.Instance)
            .HandleAsync(c.Session, Register(now: 100, max: 1000, hours: 2));

        Assert.Equal(0, c.Svc.RegisterCalls);
        Assert.Equal((byte)AuctionResultMessage.NotEnoughZenyFee, MsgFlag(c));
    }

    [Fact]
    public async Task Bid_without_enough_zeny_reports_message_and_skips_service()
    {
        var c = Build(zeny: 50);
        await new AuctionBidHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionBidHandler>.Instance)
            .HandleAsync(c.Session, Bid(auctionId: 5, money: 1000));

        Assert.Equal(0, c.Svc.BidCalls);
        Assert.Equal((byte)AuctionResultMessage.NotEnoughZenyBid, MsgFlag(c));
    }

    [Fact]
    public async Task Bid_with_enough_zeny_calls_service_and_confirms()
    {
        var c = Build(zeny: 5000);
        c.Svc.BidResult = true;
        await new AuctionBidHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionBidHandler>.Instance)
            .HandleAsync(c.Session, Bid(auctionId: 5, money: 1000));

        Assert.Equal((5L, 1000), c.Svc.LastBid);
        Assert.Equal((byte)AuctionResultMessage.BidSuccess, MsgFlag(c));
    }

    [Fact]
    public async Task Close_reports_ended_on_success()
    {
        var c = Build();
        c.Svc.BuyNowResult = true;
        await new AuctionCloseHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionCloseHandler>.Instance)
            .HandleAsync(c.Session, Close(auctionId: 7));

        Assert.Equal(7L, c.Svc.LastBuyNow);
        var ack = c.Out(x => Hdr(x) == (ushort)PacketHeader.ZC_AUCTION_ACK_MY_SELL_STOP).Single();
        Assert.Equal(0, BitConverter.ToInt16(ack, 2)); // 0 = ended
    }

    [Fact]
    public async Task Search_renders_results_from_the_service()
    {
        var c = Build();
        c.Svc.ListResult = new AuctionRequestListResponse { Success = true, Pages = 1 };
        c.Svc.ListResult.Auctions.Add(new AuctionData
        {
            AuctionId = 42, SellerName = "Seller", ItemId = (int)SwordId, ItemType = 1,
            Refine = 5, Price = 100, BuyNow = 1000, BuyerName = "", EndTimeUnix = 1700000000,
        });

        await new AuctionSearchHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionSearchHandler>.Instance)
            .HandleAsync(c.Session, Search(type: 0, idOrPrice: 0, text: "", page: 1));

        Assert.Equal((0, 0, "", 1), c.Svc.LastList);
        var res = c.Out(x => Hdr(x) == (ushort)PacketHeader.ZC_AUCTION_ITEM_REQ_SEARCH).Single();
        // header.W len.W pages.L count.L then entry: auctionId.L @12
        Assert.Equal(1, BitConverter.ToInt32(res, 4));   // pages
        Assert.Equal(1, BitConverter.ToInt32(res, 8));   // count
        Assert.Equal(42u, BitConverter.ToUInt32(res, 12)); // auction id
        Assert.Equal((ushort)SwordId, BitConverter.ToUInt16(res, 40)); // nameId @28+12
    }

    [Fact]
    public async Task MyInfo_offsets_type_by_six()
    {
        var c = Build();
        c.Svc.ListResult = new AuctionRequestListResponse { Success = true, Pages = 0 };
        await new AuctionReqMyInfoHandler(c.Reg, c.Svc, c.Client, NullLogger<AuctionReqMyInfoHandler>.Instance)
            .HandleAsync(c.Session, MyInfo(type: 0)); // my selling → 6

        Assert.Equal((6, 0, "", 1), c.Svc.LastList);
        Assert.Contains(c.OutAll(), x => Hdr(x) == (ushort)PacketHeader.ZC_AUCTION_ITEM_REQ_SEARCH);
    }

    // --- harness ---

    private sealed class Ctx
    {
        public required EntityRegistry Reg;
        public required AuctionClientService Client;
        public required FakeAuction Svc;
        public required FakeItems Items;
        public required PlayerEntity Pc;
        public required MapSessionData Session;

        public IEnumerable<byte[]> OutAll()
        {
            var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f?.GetValue(Session) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
        }
        public IEnumerable<byte[]> Out(Func<byte[], bool> pred) => OutAll().Where(pred);
    }

    private static Ctx Build(uint zeny, params InventoryItem[] inv)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var reg = new EntityRegistry(new World(map));
        var pc = new PlayerEntity(1, 7, "Seller", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        reg.Add(pc);
        var session = new MapSessionData(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 7, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned,
            Inventory = inv.ToList(), CharacterData = new CharacterDataResponse { Zeny = zeny },
        };
        var sessions = new FakeSessions(pc.Id, session);
        return new Ctx
        {
            Reg = reg, Pc = pc, Session = session, Items = new FakeItems(),
            Client = new AuctionClientService(sessions, NullLogger<AuctionClientService>.Instance),
            Svc = new FakeAuction(),
        };
    }

    private static Ctx Build(params InventoryItem[] inv) => Build(1_000_000, inv);

    private static InventoryItem Item(int slot, uint nameId, uint amount)
        => new() { Id = slot + 1, ServerIndex = slot, NameId = nameId, Amount = amount, Identified = true };

    private static ushort Hdr(byte[] b) => (ushort)(b[0] | (b[1] << 8));
    private static byte MsgFlag(Ctx c) => c.Out(x => Hdr(x) == (ushort)PacketHeader.ZC_AUCTION_RESULT).Single()[2];

    private static CZ_AUCTION_ADD_ITEM SetItem(short clientIndex)
        => Set(new CZ_AUCTION_ADD_ITEM(), ("Index", clientIndex), ("Amount", 1));
    private static CZ_AUCTION_ADD Register(int now, int max, short hours)
        => Set(new CZ_AUCTION_ADD(), ("NowMoney", now), ("MaxMoney", max), ("Hours", hours));
    private static CZ_AUCTION_BUY Bid(uint auctionId, int money)
        => Set(new CZ_AUCTION_BUY(), ("AuctionId", auctionId), ("Money", money));
    private static CZ_AUCTION_REQ_MY_SELL_STOP Close(uint auctionId)
        => Set(new CZ_AUCTION_REQ_MY_SELL_STOP(), ("AuctionId", auctionId));
    private static CZ_AUCTION_ITEM_SEARCH Search(short type, uint idOrPrice, string text, short page)
        => Set(new CZ_AUCTION_ITEM_SEARCH(), ("Type", type), ("AuctionId", idOrPrice), ("Text", text), ("Page", page));
    private static CZ_AUCTION_REQ_MY_INFO MyInfo(short type)
        => Set(new CZ_AUCTION_REQ_MY_INFO(), ("Type", type));

    private static T Set<T>(T p, params (string name, object val)[] props)
    {
        foreach (var (name, val) in props) typeof(T).GetProperty(name)!.SetValue(p, val);
        return p;
    }

    private sealed class FakeAuction : IAuctionService
    {
        public int RegisterCalls, BidCalls, BuyNowCalls, CancelCalls, ListCalls;
        public long RegisterResult; public bool BidResult, BuyNowResult, CancelResult;
        public AuctionRequestListResponse? ListResult;
        public (int idx, int amt, int now, int max, int hours) LastRegister;
        public (long id, int bid) LastBid;
        public long LastBuyNow;
        public (int type, int price, string text, int page) LastList;

        public Task<long> RegisterAsync(PlayerEntity seller, int inventoryIndex, int amount, int startPrice, int buyNowPrice, int hours, CancellationToken ct = default)
        { RegisterCalls++; LastRegister = (inventoryIndex, amount, startPrice, buyNowPrice, hours); return Task.FromResult(RegisterResult); }
        public Task<bool> BidAsync(PlayerEntity bidder, long auctionId, int bid, CancellationToken ct = default)
        { BidCalls++; LastBid = (auctionId, bid); return Task.FromResult(BidResult); }
        public Task<bool> BuyNowAsync(PlayerEntity buyer, long auctionId, CancellationToken ct = default)
        { BuyNowCalls++; LastBuyNow = auctionId; return Task.FromResult(BuyNowResult); }
        public Task<bool> CancelAsync(PlayerEntity seller, long auctionId, CancellationToken ct = default)
        { CancelCalls++; CancelResult = true; return Task.FromResult(true); }
        public Task<AuctionRequestListResponse?> RequestListAsync(PlayerEntity searcher, int type, int price, string search, int page, CancellationToken ct = default)
        { ListCalls++; LastList = (type, price, search ?? "", page); return Task.FromResult(ListResult); }
    }

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly EntityId _id; private readonly MapSessionData _s;
        public FakeSessions(EntityId id, MapSessionData s) { _id = id; _s = s; }
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId.Value == _id.Value ? _s : null;
        public MapSessionData? GetByAccountId(int accountId) => _s;
    }

    private sealed class FakeItems : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _db = new()
        {
            [SwordId] = new ItemEntity { Id = SwordId, NameAegis = "Sword", Type = "Weapon" },
            [PotionId] = new ItemEntity { Id = PotionId, NameAegis = "Red_Potion", Type = "Usable" },
        };
        public int Count => _db.Count;
        public ItemEntity? Get(uint itemId) => _db.GetValueOrDefault(itemId);
        public ItemEntity? GetByAegisName(string aegisName) => _db.Values.FirstOrDefault(i => i.NameAegis == aegisName);
        public IEnumerable<ItemEntity> All() => _db.Values;
        public void Reload() { }
    }

    private sealed class World : IMapWorldRegistry
    {
        private readonly MapData _m;
        public World(MapData m) => _m = m;
        public MapData? Get(string name) => _m;
        public IEnumerable<MapData> All => new[] { _m };
        public int TotalCells => _m.CellCount;
        public bool Contains(string name) => true;
    }
}
