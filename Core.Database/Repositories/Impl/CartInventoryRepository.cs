using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class CartInventoryRepository : BaseRepository<CartInventoryEntity>, ICartInventoryRepository
{
    public CartInventoryRepository(GameDbContext context) : base(context) {}
    
    public async Task<CartInventoryEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);
    
    public async Task<IReadOnlyList<CartInventoryEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public new async Task<CartInventoryEntity> AddAsync(CartInventoryEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(CartInventoryEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
