using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

// Read-only accessors over the static catalog tables. Each repo is
// thin — runtime services (`AbraDatabase`, `QuestService`, etc.) call
// GetAllAsync once on Reload() and cache in-memory.

public interface IAbraDbRepository
{
    Task<IReadOnlyList<AbraDbEntity>> GetAllAsync(CancellationToken ct = default);
}

public interface IMagicMushroomDbRepository
{
    Task<IReadOnlyList<MagicMushroomDbEntity>> GetAllAsync(CancellationToken ct = default);
}

public interface ISpellbookDbRepository
{
    Task<IReadOnlyList<SpellbookDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<SpellbookDbEntity?> GetByBookAsync(string bookNameAegis, CancellationToken ct = default);
}

public interface IQuestDbRepository
{
    Task<IReadOnlyList<QuestDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<QuestDbEntity?> GetByIdAsync(uint questId, CancellationToken ct = default);
}

public interface IPetDbRepository
{
    Task<IReadOnlyList<PetDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<PetDbEntity?> GetByMobAsync(string mobAegis, CancellationToken ct = default);
}

public interface IAchievementDbRepository
{
    Task<IReadOnlyList<AchievementDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<AchievementDbEntity?> GetByIdAsync(uint achievementId, CancellationToken ct = default);
}

public interface IHomunculusDbRepository
{
    Task<IReadOnlyList<HomunculusDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<HomunculusDbEntity?> GetByClassAsync(string classAegis, CancellationToken ct = default);
}

public interface IMercenaryDbRepository
{
    Task<IReadOnlyList<MercenaryDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<MercenaryDbEntity?> GetByIdAsync(uint mercId, CancellationToken ct = default);
}

public interface IInstanceDbRepository
{
    Task<IReadOnlyList<InstanceDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<InstanceDbEntity?> GetByIdAsync(uint instanceId, CancellationToken ct = default);
}

/// <summary>AT-F: per-merc-class skill grants.</summary>
public interface IMercenarySkillDbRepository
{
    Task<IReadOnlyList<MercenarySkillDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MercenarySkillDbEntity>> GetByMercAsync(uint mercId, CancellationToken ct = default);
}

/// <summary>AT-F: per-homunculus-class skill tree.</summary>
public interface IHomunculusSkillTreeDbRepository
{
    Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetByClassAsync(string classAegis, CancellationToken ct = default);
}

// Battleground catalog: read via the existing CatalogEntities.BattlegroundDbEntity
// JSON-payload table (DB-5). The typed consumer is DB-8 territory.
