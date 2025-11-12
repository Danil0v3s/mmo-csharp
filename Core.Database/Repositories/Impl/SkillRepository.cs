using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class SkillRepository : BaseRepository<SkillEntity>, ISkillRepository
{
    public SkillRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<SkillEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public async Task<SkillEntity?> GetByCharIdAndSkillIdAsync(int charId, ushort skillId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { charId, skillId }, ct);
    
    public new async Task<SkillEntity> AddAsync(SkillEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(SkillEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int charId, ushort skillId, CancellationToken ct = default) {
        var entity = await GetByCharIdAndSkillIdAsync(charId, skillId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
