using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<SkillEntity?> GetByCharIdAndSkillIdAsync(int charId, ushort skillId, CancellationToken ct = default);
    Task<SkillEntity> AddAsync(SkillEntity entity, CancellationToken ct = default);
    Task UpdateAsync(SkillEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int charId, ushort skillId, CancellationToken ct = default);
}

