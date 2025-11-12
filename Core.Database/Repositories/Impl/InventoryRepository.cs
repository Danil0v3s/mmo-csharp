using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class InventoryRepository : BaseRepository<InventoryEntity>, IInventoryRepository
{
    public InventoryRepository(GameDbContext context) : base(context) {}
    
    public async Task<InventoryEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);
    
    public async Task<IReadOnlyList<InventoryEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public async Task<IReadOnlyList<InventoryEntity>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<InventoryEntity> AddAsync(InventoryEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(InventoryEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
    
    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        await DbSet.AnyAsync(e => e.Id == id, ct);
}
