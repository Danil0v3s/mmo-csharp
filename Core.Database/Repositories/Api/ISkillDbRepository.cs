using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

/// <summary>
/// Read-only access to the static <c>skill_db</c> catalog. Mirror of
/// rAthena's <c>use_sql_db: yes</c> path for skill data — same shape
/// as <see cref="IItemRepository"/> and <see cref="IMobRepository"/>.
///
/// Until the rAthena skill_db.yml → SQL seed lands, the consumer side
/// (<c>Map.Server.Skills.SkillDb</c>) falls back to its hand-built
/// starter catalog when this repository returns no rows.
/// </summary>
public interface ISkillDbRepository
{
    Task<SkillDbEntity?> GetByIdAsync(ushort skillId, CancellationToken ct = default);
    Task<SkillDbEntity?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<SkillDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
