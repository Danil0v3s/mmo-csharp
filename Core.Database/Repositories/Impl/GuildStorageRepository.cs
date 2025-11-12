using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class GuildStorageRepository : BaseRepository<GuildStorageEntity>, IGuildStorageRepository
{
    public GuildStorageRepository(GameDbContext context) : base(context) {}
    
    public async Task<GuildStorageEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);
    
    public async Task<IReadOnlyList<GuildStorageEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.GuildId == guildId).ToListAsync(ct);
    
    public new async Task<GuildStorageEntity> AddAsync(GuildStorageEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(GuildStorageEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int id, CancellationToken ct = default) {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
