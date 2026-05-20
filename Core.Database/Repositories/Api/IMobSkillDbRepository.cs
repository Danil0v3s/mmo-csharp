using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

/// <summary>
/// Read-only accessor over the <c>mob_skill_db</c> table. The
/// catalog is seeded from rAthena's pre-generated SQL at boot.
/// </summary>
public interface IMobSkillDbRepository
{
    /// <summary>Every row in the catalog.</summary>
    Task<IReadOnlyList<MobSkillDbEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>All skills configured for a single mob.</summary>
    Task<IReadOnlyList<MobSkillDbEntity>> GetByMobIdAsync(int mobId, CancellationToken ct = default);

    /// <summary>Total row count (sanity-check the seed loaded).</summary>
    Task<int> CountAsync(CancellationToken ct = default);
}
