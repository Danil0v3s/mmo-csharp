using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.5 — verifies IntifService dispatches the auction round-trip
/// (RequestList / Register / Cancel / Close / Bid) through
/// ICharServerIpcServiceAuction when wired. 5 dispatch tests
/// covering each entry point with arg pass-through assertions.
/// </summary>
public class IntifAuctionWiringTests
{
    [Fact]
    public void AuctionRequestList_WithIpc_Dispatches()
    {
        var fake = new RecordingAuctionIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, auctionIpc: fake);
        Assert.Equal(1, intif.AuctionRequestList(charId: 7, type: 1, price: 1000,
            search: "Sword", page: 2));
        Assert.Single(fake.ListCalls);
        Assert.Equal(7L, fake.ListCalls[0].CharacterId);
        Assert.Equal(1, fake.ListCalls[0].Type);
        Assert.Equal(1000, fake.ListCalls[0].Price);
        Assert.Equal("Sword", fake.ListCalls[0].Search);
        Assert.Equal(2, fake.ListCalls[0].Page);
    }

    [Fact]
    public void AuctionRegister_WithIpc_PacksAndDispatches()
    {
        var fake = new RecordingAuctionIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, auctionIpc: fake);
        Assert.Equal(1, intif.AuctionRegister(
            charId: 7, type: 0, sellerCharId: 7, sellerName: "Bob",
            now: 0, hours: 24, priceStart: 1000, priceBuyNow: 5000,
            itemId: 1201, refine: 4, attribute: 0, identify: 1, amount: 1));
        Assert.Single(fake.RegisterCalls);
        var a = fake.RegisterCalls[0];
        Assert.Equal(7L, a.SellerCharacterId);
        Assert.Equal("Bob", a.SellerName);
        Assert.Equal(1201, a.ItemId);
        Assert.Equal(4, a.Refine);
        Assert.Equal(1000, a.Price);
        Assert.Equal(5000, a.BuyNow);
        Assert.Equal(24, a.Hours);
    }

    [Fact]
    public void AuctionCancel_WithIpc_Dispatches()
    {
        var fake = new RecordingAuctionIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, auctionIpc: fake);
        Assert.Equal(1, intif.AuctionCancel(charId: 7, auctionId: 99u));
        Assert.Single(fake.CancelCalls);
        Assert.Equal(99L, fake.CancelCalls[0].AuctionId);
    }

    [Fact]
    public void AuctionClose_WithIpc_Dispatches()
    {
        var fake = new RecordingAuctionIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, auctionIpc: fake);
        Assert.Equal(1, intif.AuctionClose(charId: 7, auctionId: 101u));
        Assert.Single(fake.CloseCalls);
        Assert.Equal(101L, fake.CloseCalls[0].AuctionId);
    }

    [Fact]
    public void AuctionBid_WithIpc_Dispatches()
    {
        var fake = new RecordingAuctionIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, auctionIpc: fake);
        Assert.Equal(1, intif.AuctionBid(charId: 7, auctionId: 50u, bid: 2500, bidder: "Alice"));
        Assert.Single(fake.BidCalls);
        Assert.Equal(50L, fake.BidCalls[0].AuctionId);
        Assert.Equal(2500, fake.BidCalls[0].Bid);
        Assert.Equal("Alice", fake.BidCalls[0].Bidder);
    }

    private sealed class RecordingAuctionIpc : ICharServerIpcServiceAuction
    {
        public sealed record ListCall(long CharacterId, int Type, int Price, string Search, int Page);
        public sealed record CancelCall(long CharacterId, long AuctionId);
        public sealed record CloseCall(long CharacterId, long AuctionId);
        public sealed record BidCall(long CharacterId, string Bidder, long AuctionId, int Bid);

        public List<ListCall> ListCalls { get; } = new();
        public List<Core.Server.IPC.AuctionData> RegisterCalls { get; } = new();
        public List<CancelCall> CancelCalls { get; } = new();
        public List<CloseCall> CloseCalls { get; } = new();
        public List<BidCall> BidCalls { get; } = new();

        public Task<Core.Server.IPC.AuctionRequestListResponse?> AuctionRequestListAsync(
            long characterId, int type, int price, string searchText, int page,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add(new ListCall(characterId, type, price, searchText, page));
            return Task.FromResult<Core.Server.IPC.AuctionRequestListResponse?>(null);
        }

        public Task<Core.Server.IPC.AuctionRegisterResponse?> AuctionRegisterAsync(
            Core.Server.IPC.AuctionData auction, CancellationToken cancellationToken = default)
        {
            RegisterCalls.Add(auction);
            return Task.FromResult<Core.Server.IPC.AuctionRegisterResponse?>(null);
        }

        public Task<Core.Server.IPC.AuctionCancelResponse?> AuctionCancelAsync(
            long characterId, long auctionId, CancellationToken cancellationToken = default)
        {
            CancelCalls.Add(new CancelCall(characterId, auctionId));
            return Task.FromResult<Core.Server.IPC.AuctionCancelResponse?>(null);
        }

        public Task<Core.Server.IPC.AuctionCloseResponse?> AuctionCloseAsync(
            long characterId, long auctionId, CancellationToken cancellationToken = default)
        {
            CloseCalls.Add(new CloseCall(characterId, auctionId));
            return Task.FromResult<Core.Server.IPC.AuctionCloseResponse?>(null);
        }

        public Task<Core.Server.IPC.AuctionBidResponse?> AuctionBidAsync(
            long characterId, string bidderName, long auctionId, int bid,
            CancellationToken cancellationToken = default)
        {
            BidCalls.Add(new BidCall(characterId, bidderName, auctionId, bid));
            return Task.FromResult<Core.Server.IPC.AuctionBidResponse?>(null);
        }
    }
}
