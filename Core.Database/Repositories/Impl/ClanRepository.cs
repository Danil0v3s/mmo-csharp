using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class ClanRepository : BaseRepository<ClanEntity>, IClanRepository
{
    public ClanRepository(GameDbContext context) : base(context) {}
    
    public async Task<ClanEntity?> GetByIdAsync(int clanId, CancellationToken ct = default) =>
        await DbSet.Include(c => c.Members).FirstOrDefaultAsync(c => c.ClanId == clanId, ct);
    
    public async Task<ClanEntity?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(c => c.Name == name, ct);
    
    public async Task<IReadOnlyList<ClanEntity>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<ClanEntity> AddAsync(ClanEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(ClanEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int clanId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { clanId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
