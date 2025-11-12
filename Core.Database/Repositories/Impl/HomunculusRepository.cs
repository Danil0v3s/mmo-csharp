using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class HomunculusRepository : BaseRepository<HomunculusEntity>, IHomunculusRepository
{
    public HomunculusRepository(GameDbContext context) : base(context) {}
    
    public async Task<HomunculusEntity?> GetByIdAsync(int homunId, CancellationToken ct = default) =>
        await DbSet.Include(h => h.Skills).FirstOrDefaultAsync(h => h.HomunId == homunId, ct);
    
    public async Task<HomunculusEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Include(h => h.Skills).FirstOrDefaultAsync(h => h.CharId == charId, ct);
    
    public new async Task<HomunculusEntity> AddAsync(HomunculusEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(HomunculusEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int homunId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { homunId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
