using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class AchievementRepository : BaseRepository<AchievementEntity>, IAchievementRepository
{
    public AchievementRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<AchievementEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public async Task<AchievementEntity?> GetByCharIdAndAchievementIdAsync(int charId, long achievementId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { charId, achievementId }, ct);
    
    public new async Task<AchievementEntity> AddAsync(AchievementEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(AchievementEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int charId, long achievementId, CancellationToken ct = default) {
        var entity = await GetByCharIdAndAchievementIdAsync(charId, achievementId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
