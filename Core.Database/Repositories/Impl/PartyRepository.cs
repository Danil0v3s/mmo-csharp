using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class PartyRepository : BaseRepository<PartyEntity>, IPartyRepository
{
    public PartyRepository(GameDbContext context) : base(context) {}
    
    public async Task<PartyEntity?> GetByIdAsync(int partyId, CancellationToken ct = default) =>
        await DbSet.Include(p => p.Members).FirstOrDefaultAsync(p => p.PartyId == partyId, ct);
    
    public async Task<PartyEntity?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(p => p.Name == name, ct);
    
    public async Task<IReadOnlyList<PartyEntity>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<PartyEntity> AddAsync(PartyEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(PartyEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int partyId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { partyId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
