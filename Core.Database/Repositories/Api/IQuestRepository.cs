using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IQuestRepository
{
    Task<IReadOnlyList<QuestEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<QuestEntity?> GetByCharIdAndQuestIdAsync(int charId, uint questId, CancellationToken ct = default);
    Task<QuestEntity> AddAsync(QuestEntity entity, CancellationToken ct = default);
    Task UpdateAsync(QuestEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int charId, uint questId, CancellationToken ct = default);
}

