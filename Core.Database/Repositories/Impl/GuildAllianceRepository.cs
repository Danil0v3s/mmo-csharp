using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class GuildAllianceRepository : BaseRepository<GuildAllianceEntity>, IGuildAllianceRepository
{
    public GuildAllianceRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<GuildAllianceEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.GuildId == guildId).ToListAsync(ct);
    
    public new async Task<GuildAllianceEntity> AddAsync(GuildAllianceEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public async Task DeleteAsync(int guildId, int allianceId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { guildId, allianceId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
