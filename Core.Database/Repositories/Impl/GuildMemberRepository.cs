using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class GuildMemberRepository : BaseRepository<GuildMemberEntity>, IGuildMemberRepository
{
    public GuildMemberRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<GuildMemberEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.GuildId == guildId).ToListAsync(ct);
    
    public async Task<GuildMemberEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(e => e.CharId == charId, ct);
    
    public new async Task<GuildMemberEntity> AddAsync(GuildMemberEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(GuildMemberEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int guildId, int charId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { guildId, charId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
