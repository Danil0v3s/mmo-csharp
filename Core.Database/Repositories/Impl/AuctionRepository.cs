using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class AuctionRepository : BaseRepository<AuctionEntity>, IAuctionRepository
{
    public AuctionRepository(GameDbContext context) : base(context) {}
    
    public async Task<AuctionEntity?> GetByIdAsync(long auctionId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { auctionId }, ct);
    
    public async Task<IReadOnlyList<AuctionEntity>> GetActiveAsync(CancellationToken ct = default) =>
        await DbSet.Where(a => a.BuyerId == 0).ToListAsync(ct);
    
    public async Task<IReadOnlyList<AuctionEntity>> GetBySellerIdAsync(int sellerId, CancellationToken ct = default) =>
        await DbSet.Where(a => a.SellerId == sellerId).ToListAsync(ct);
    
    public new async Task<AuctionEntity> AddAsync(AuctionEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(AuctionEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(long auctionId, CancellationToken ct = default) {
        var entity = await GetByIdAsync(auctionId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
