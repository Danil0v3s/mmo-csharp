using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IAchievementRepository
{
    Task<IReadOnlyList<AchievementEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<AchievementEntity?> GetByCharIdAndAchievementIdAsync(int charId, long achievementId, CancellationToken ct = default);
    Task<AchievementEntity> AddAsync(AchievementEntity entity, CancellationToken ct = default);
    Task UpdateAsync(AchievementEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int charId, long achievementId, CancellationToken ct = default);
}

