using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class BuyingStoreRepository : BaseRepository<BuyingStoreEntity>, IBuyingStoreRepository
{
    public BuyingStoreRepository(GameDbContext context) : base(context) {}
    
    public async Task<BuyingStoreEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id, ct);
    
    public async Task<IReadOnlyList<BuyingStoreEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(b => b.CharId == charId).ToListAsync(ct);
    
    public async Task<IReadOnlyList<BuyingStoreEntity>> GetAllActiveAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<BuyingStoreEntity> AddAsync(BuyingStoreEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(BuyingStoreEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { id }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
