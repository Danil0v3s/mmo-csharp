using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class ElementalRepository : BaseRepository<ElementalEntity>, IElementalRepository
{
    public ElementalRepository(GameDbContext context) : base(context) {}
    
    public async Task<ElementalEntity?> GetByIdAsync(int eleId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { eleId }, ct);
    
    public async Task<ElementalEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(e => e.CharId == charId, ct);
    
    public new async Task<ElementalEntity> AddAsync(ElementalEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(ElementalEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int eleId, CancellationToken ct = default) {
        var entity = await GetByIdAsync(eleId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
