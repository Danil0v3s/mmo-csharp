using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class StorageRepository : BaseRepository<StorageEntity>, IStorageRepository
{
    public StorageRepository(GameDbContext context) : base(context) {}
    
    public async Task<StorageEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);
    
    public async Task<IReadOnlyList<StorageEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.AccountId == accountId).ToListAsync(ct);
    
    public new async Task<StorageEntity> AddAsync(StorageEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(StorageEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
