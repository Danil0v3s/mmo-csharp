using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class GuildRepository : BaseRepository<GuildEntity>, IGuildRepository
{
    public GuildRepository(GameDbContext context) : base(context) {}
    
    public async Task<GuildEntity?> GetByIdAsync(int guildId, CancellationToken ct = default) =>
        await DbSet.Include(g => g.Members).FirstOrDefaultAsync(g => g.GuildId == guildId, ct);
    
    public async Task<GuildEntity?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(g => g.Name == name, ct);
    
    public async Task<IReadOnlyList<GuildEntity>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);
    
    public new async Task<GuildEntity> AddAsync(GuildEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(GuildEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int guildId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { guildId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
    
    public async Task<bool> ExistsAsync(int guildId, CancellationToken ct = default) =>
        await DbSet.AnyAsync(g => g.GuildId == guildId, ct);
}
