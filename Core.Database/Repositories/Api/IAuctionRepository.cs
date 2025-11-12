using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IAuctionRepository
{
    Task<AuctionEntity?> GetByIdAsync(long auctionId, CancellationToken ct = default);
    Task<IReadOnlyList<AuctionEntity>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuctionEntity>> GetBySellerIdAsync(int sellerId, CancellationToken ct = default);
    Task<AuctionEntity> AddAsync(AuctionEntity entity, CancellationToken ct = default);
    Task UpdateAsync(AuctionEntity entity, CancellationToken ct = default);
    Task DeleteAsync(long auctionId, CancellationToken ct = default);
}

