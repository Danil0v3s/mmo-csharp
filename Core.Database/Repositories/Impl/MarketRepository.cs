using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class MarketRepository : BaseRepository<MarketEntity>, IMarketRepository
{
    public MarketRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<MarketEntity>> GetByNameAsync(string name, CancellationToken ct = default) =>
        await DbSet.Where(m => m.Name == name).ToListAsync(ct);
    
    public new async Task<MarketEntity> AddAsync(MarketEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(MarketEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(string name, uint nameId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { name, nameId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
