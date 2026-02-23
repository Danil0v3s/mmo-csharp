using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<AuctionRequestListResponse?> AuctionRequestListAsync(
        long characterId,
        int type,
        int price,
        string searchText,
        int page,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AuctionRequestListAsync(new AuctionRequestListRequest
        {
            CharacterId = characterId,
            Type = type,
            Price = price,
            SearchText = searchText ?? string.Empty,
            Page = page
        }, cancellationToken: cancellationToken);
    }

    public async Task<AuctionRegisterResponse?> AuctionRegisterAsync(
        AuctionData auction,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AuctionRegisterAsync(new AuctionRegisterRequest
        {
            Auction = auction ?? new AuctionData()
        }, cancellationToken: cancellationToken);
    }

    public async Task<AuctionCancelResponse?> AuctionCancelAsync(
        long characterId,
        long auctionId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AuctionCancelAsync(new AuctionCancelRequest
        {
            CharacterId = characterId,
            AuctionId = auctionId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AuctionCloseResponse?> AuctionCloseAsync(
        long characterId,
        long auctionId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AuctionCloseAsync(new AuctionCloseRequest
        {
            CharacterId = characterId,
            AuctionId = auctionId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AuctionBidResponse?> AuctionBidAsync(
        long characterId,
        string bidderName,
        long auctionId,
        int bid,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AuctionBidAsync(new AuctionBidRequest
        {
            CharacterId = characterId,
            BidderName = bidderName ?? string.Empty,
            AuctionId = auctionId,
            Bid = bid
        }, cancellationToken: cancellationToken);
    }
}
