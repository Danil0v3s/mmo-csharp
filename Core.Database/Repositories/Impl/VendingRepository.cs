using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class VendingRepository : BaseRepository<VendingEntity>, IVendingRepository
{
    public VendingRepository(GameDbContext context) : base(context) {}
    
    public async Task<VendingEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.Include(v => v.Items).FirstOrDefaultAsync(v => v.Id == id, ct);
    
    public async Task<IReadOnlyList<VendingEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(v => v.CharId == charId).ToListAsync(ct);
    
    public async Task<IReadOnlyList<VendingEntity>> GetAllActiveAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<VendingEntity> AddAsync(VendingEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(VendingEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { id }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
