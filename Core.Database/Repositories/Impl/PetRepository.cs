using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class PetRepository : BaseRepository<PetEntity>, IPetRepository
{
    public PetRepository(GameDbContext context) : base(context) {}
    
    public async Task<PetEntity?> GetByIdAsync(int petId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { petId }, ct);
    
    public async Task<IReadOnlyList<PetEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public new async Task<PetEntity> AddAsync(PetEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(PetEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int petId, CancellationToken ct = default) {
        var entity = await GetByIdAsync(petId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
