using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceAuction
{
    Task<AuctionRequestListResponse?> AuctionRequestListAsync(
        long characterId,
        int type,
        int price,
        string searchText,
        int page,
        CancellationToken cancellationToken = default);

    Task<AuctionRegisterResponse?> AuctionRegisterAsync(
        AuctionData auction,
        CancellationToken cancellationToken = default);

    Task<AuctionCancelResponse?> AuctionCancelAsync(
        long characterId,
        long auctionId,
        CancellationToken cancellationToken = default);

    Task<AuctionCloseResponse?> AuctionCloseAsync(
        long characterId,
        long auctionId,
        CancellationToken cancellationToken = default);

    Task<AuctionBidResponse?> AuctionBidAsync(
        long characterId,
        string bidderName,
        long auctionId,
        int bid,
        CancellationToken cancellationToken = default);
}
