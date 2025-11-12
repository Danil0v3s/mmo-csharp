using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class MercenaryRepository : BaseRepository<MercenaryEntity>, IMercenaryRepository
{
    public MercenaryRepository(GameDbContext context) : base(context) {}
    
    public async Task<MercenaryEntity?> GetByIdAsync(int merId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { merId }, ct);
    
    public async Task<MercenaryEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(e => e.CharId == charId, ct);
    
    public new async Task<MercenaryEntity> AddAsync(MercenaryEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(MercenaryEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int merId, CancellationToken ct = default) {
        var entity = await GetByIdAsync(merId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
