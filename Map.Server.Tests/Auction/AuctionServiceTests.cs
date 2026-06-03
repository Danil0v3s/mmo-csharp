using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Services;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Auction;

/// <summary>
/// FEATURE-06 — auction map-side escrow / dispatch / gates.
/// </summary>
public class AuctionServiceTests
{
    private const uint SwordId = 1101;

    private static (AuctionService svc, PlayerEntity pc, MapSessionData session, FakeAuctionIpc ipc) Build(
        uint zeny = 1_000_000, params InventoryItem[] inv)
    {
        var pc = new PlayerEntity(1, 7, "Seller", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 7, CharacterId = 1, EntityId = pc.Id,
            Inventory = inv.ToList(),
            CharacterData = new CharacterDataResponse { Zeny = zeny },
        };
        var ipc = new FakeAuctionIpc();
        var svc = new AuctionService(NullLogger<AuctionService>.Instance, new FakeSessions(session), ipc, items: null);
        return (svc, pc, session, ipc);
    }

    private static InventoryItem Item(int slot, uint amount) =>
        new() { Id = slot + 1, ServerIndex = slot, NameId = SwordId, Amount = amount, Identified = true };

    // --- Register ---

    [Fact]
    public async Task Register_escrows_item_and_fee_and_dispatches()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1_000_000, inv: Item(0, 1));
        ipc.NextAuctionId = 555;

        var id = await svc.RegisterAsync(pc, inventoryIndex: 0, amount: 1, startPrice: 100, buyNowPrice: 1000, hours: 2);

        Assert.Equal(555, id);
        Assert.Empty(session.Inventory!);                       // item escrowed out
        Assert.Equal(1_000_000u - 2 * 12000u, session.CharacterData!.Zeny); // 2h fee
        Assert.Equal(1, ipc.RegisterCalls);
    }

    [Fact]
    public async Task Register_rejects_bad_price_or_hours_without_escrow()
    {
        var (svc, pc, session, ipc) = Build(inv: Item(0, 1));
        Assert.Equal(0, await svc.RegisterAsync(pc, 0, 1, startPrice: 1000, buyNowPrice: 100, hours: 2)); // start >= buynow
        Assert.Equal(0, await svc.RegisterAsync(pc, 0, 1, 100, 1000, hours: 99));                          // hours > 48
        Assert.Single(session.Inventory!);                  // never escrowed
        Assert.Equal(0, ipc.RegisterCalls);
    }

    [Fact]
    public async Task Register_insufficient_fee_returns_zero()
    {
        var (svc, pc, session, ipc) = Build(zeny: 100, inv: Item(0, 1)); // 2h fee = 24000 > 100
        Assert.Equal(0, await svc.RegisterAsync(pc, 0, 1, 100, 1000, hours: 2));
        Assert.Single(session.Inventory!);
        Assert.Equal(100u, session.CharacterData!.Zeny);
        Assert.Equal(0, ipc.RegisterCalls);
    }

    [Fact]
    public async Task Register_char_reject_rebounds_item_and_fee()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1_000_000, inv: Item(0, 1));
        ipc.FailRegister = true;

        Assert.Equal(0, await svc.RegisterAsync(pc, 0, 1, 100, 1000, hours: 2));
        Assert.Single(session.Inventory!);                  // rebounded
        Assert.Equal(1_000_000u, session.CharacterData!.Zeny); // fee rebounded
    }

    // --- Bid ---

    [Fact]
    public async Task Bid_below_current_high_is_rejected_without_debit()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1_000_000);
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, Price = 500, BuyNow = 5000, SellerCharacterId = 99 });

        Assert.False(await svc.BidAsync(pc, 9, bid: 400)); // below the 500 high
        Assert.Equal(1_000_000u, session.CharacterData!.Zeny);
        Assert.Equal(0, ipc.BidCalls);
    }

    [Fact]
    public async Task Bid_valid_debits_and_dispatches()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1_000_000);
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, Price = 500, BuyNow = 5000, SellerCharacterId = 99 });

        Assert.True(await svc.BidAsync(pc, 9, bid: 800));
        Assert.Equal(1_000_000u - 800u, session.CharacterData!.Zeny);
        Assert.Equal(1, ipc.BidCalls);
    }

    [Fact]
    public async Task Bid_on_own_auction_rejected()
    {
        var (svc, pc, _, ipc) = Build(zeny: 1_000_000);
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, Price = 0, BuyNow = 5000, SellerCharacterId = pc.CharacterId });
        Assert.False(await svc.BidAsync(pc, 9, bid: 800));
        Assert.Equal(0, ipc.BidCalls);
    }

    // --- BuyNow / Cancel / List ---

    [Fact]
    public async Task BuyNow_debits_buynow_price_and_dispatches()
    {
        var (svc, pc, session, ipc) = Build(zeny: 1_000_000);
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, Price = 100, BuyNow = 5000, SellerCharacterId = 99 });

        Assert.True(await svc.BuyNowAsync(pc, 9));
        Assert.Equal(1_000_000u - 5000u, session.CharacterData!.Zeny);
        Assert.Equal(1, ipc.CloseCalls);
    }

    [Fact]
    public async Task Cancel_with_active_bidder_is_rejected()
    {
        var (svc, pc, _, ipc) = Build();
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, BuyerCharacterId = 42, SellerCharacterId = pc.CharacterId });
        Assert.False(await svc.CancelAsync(pc, 9));
        Assert.Equal(0, ipc.CancelCalls);
    }

    [Fact]
    public async Task Cancel_without_bidder_dispatches()
    {
        var (svc, pc, _, ipc) = Build();
        svc.SeedCacheForTest(new AuctionData { AuctionId = 9, BuyerCharacterId = 0, SellerCharacterId = pc.CharacterId });
        Assert.True(await svc.CancelAsync(pc, 9));
        Assert.Equal(1, ipc.CancelCalls);
    }

    [Fact]
    public async Task RequestList_caches_response()
    {
        var (svc, pc, _, ipc) = Build();
        ipc.ListResult = new AuctionData { AuctionId = 77, Price = 100, BuyNow = 9000, SellerCharacterId = 50 };

        var resp = await svc.RequestListAsync(pc, type: 0, price: 0, search: "", page: 0);
        Assert.True(resp!.Success);
        // Cached → a buy-now off the cache works.
        Assert.True(await svc.BuyNowAsync(pc, 77));
    }

    // --- fakes ---

    private sealed class FakeSessions(MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => session;
    }

    private sealed class FakeAuctionIpc : ICharServerIpcServiceAuction
    {
        public int RegisterCalls, BidCalls, CloseCalls, CancelCalls;
        public long NextAuctionId = 1; public bool FailRegister;
        public AuctionData? ListResult;

        public Task<AuctionRegisterResponse?> AuctionRegisterAsync(AuctionData auction, CancellationToken ct = default)
        {
            RegisterCalls++;
            if (FailRegister) return Task.FromResult<AuctionRegisterResponse?>(new AuctionRegisterResponse { Success = false });
            return Task.FromResult<AuctionRegisterResponse?>(new AuctionRegisterResponse
            { Success = true, Auction = new AuctionData { AuctionId = NextAuctionId } });
        }

        public Task<AuctionBidResponse?> AuctionBidAsync(long characterId, string bidderName, long auctionId, int bid, CancellationToken ct = default)
        { BidCalls++; return Task.FromResult<AuctionBidResponse?>(new AuctionBidResponse { Success = true }); }

        public Task<AuctionCloseResponse?> AuctionCloseAsync(long characterId, long auctionId, CancellationToken ct = default)
        { CloseCalls++; return Task.FromResult<AuctionCloseResponse?>(new AuctionCloseResponse { Success = true }); }

        public Task<AuctionCancelResponse?> AuctionCancelAsync(long characterId, long auctionId, CancellationToken ct = default)
        { CancelCalls++; return Task.FromResult<AuctionCancelResponse?>(new AuctionCancelResponse { Success = true }); }

        public Task<AuctionRequestListResponse?> AuctionRequestListAsync(long characterId, int type, int price, string searchText, int page, CancellationToken ct = default)
        {
            var resp = new AuctionRequestListResponse { Success = true, Count = 1, Pages = 1 };
            if (ListResult != null) resp.Auctions.Add(ListResult);
            return Task.FromResult<AuctionRequestListResponse?>(resp);
        }
    }
}
