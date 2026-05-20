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
