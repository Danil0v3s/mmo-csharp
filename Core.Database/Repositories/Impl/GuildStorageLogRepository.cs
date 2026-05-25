using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class GuildStorageLogRepository : BaseRepository<GuildStorageLogEntity>, IGuildStorageLogRepository
{
    public GuildStorageLogRepository(GameDbContext context) : base(context) { }

    public async Task<IReadOnlyList<GuildStorageLogEntity>> GetByGuildIdAsync(int guildId, int limit = 100, CancellationToken ct = default) =>
        await DbSet.Where(e => e.GuildId == guildId)
                   .OrderByDescending(e => e.Time)
                   .Take(limit)
                   .ToListAsync(ct);

    public async Task<IReadOnlyList<GuildStorageLogEntity>> GetByGuildAndItemAsync(int guildId, uint nameId, int limit = 100, CancellationToken ct = default) =>
        await DbSet.Where(e => e.GuildId == guildId && e.NameId == nameId)
                   .OrderByDescending(e => e.Time)
                   .Take(limit)
                   .ToListAsync(ct);

    public new async Task<GuildStorageLogEntity> AddAsync(GuildStorageLogEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
}
