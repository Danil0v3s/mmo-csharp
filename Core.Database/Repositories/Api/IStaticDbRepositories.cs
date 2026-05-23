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

/// <summary>AT-G: stylist option catalog (rAthena stylist.yml).</summary>
public interface IStylistDbRepository
{
    Task<IReadOnlyList<StylistDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StylistDbEntity>> GetByLookAsync(int look, CancellationToken ct = default);
}

/// <summary>AT-G: achievement-level XP curve (rAthena achievement_level_db.yml).</summary>
public interface IAchievementLevelDbRepository
{
    Task<IReadOnlyList<AchievementLevelDbEntity>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>AT-G: per-job per-weapon ASPD base delay (rAthena job_aspd.yml).</summary>
public interface IJobAspdDbRepository
{
    Task<IReadOnlyList<JobAspdDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<JobAspdDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default);
}

/// <summary>AT-G: script constants catalog (rAthena const.yml).</summary>
public interface IConstDbRepository
{
    Task<IReadOnlyList<ConstDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ConstDbEntity?> GetByNameAsync(string name, CancellationToken ct = default);
}

// ============================================================================
// DB-8a: tier-1 re-normalized catalog repos
// ============================================================================

/// <summary>DB-8a: level-gap penalty curves (rAthena level_penalty.yml).</summary>
public interface ILevelPenaltyDbRepository
{
    Task<IReadOnlyList<LevelPenaltyDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LevelPenaltyDifferenceDbEntity>> GetDifferencesAsync(string penaltyType, CancellationToken ct = default);
    Task<IReadOnlyList<LevelPenaltyDifferenceDbEntity>> GetAllDifferencesAsync(CancellationToken ct = default);
}

/// <summary>DB-8a: elemental damage matrix (rAthena attr_fix.yml).</summary>
public interface IAttrFixDbRepository
{
    Task<IReadOnlyList<AttrFixDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<int> GetMultiplierAsync(int level, string attackerElement, string defenderElement, CancellationToken ct = default);
}

/// <summary>DB-8a: reputation faction bundles (rAthena reputation_group.yml).</summary>
public interface IReputationGroupDbRepository
{
    Task<IReadOnlyList<ReputationGroupDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ReputationGroupDbEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ReputationGroupMemberDbEntity>> GetMembersAsync(int groupId, CancellationToken ct = default);
}

// ============================================================================
// DB-8b: tier-2 single-child re-normalized catalog repos
// ============================================================================

/// <summary>DB-8b: weighted random mob groups (rAthena mob_summon.yml).</summary>
public interface IMobSummonDbRepository
{
    Task<IReadOnlyList<MobSummonDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<MobSummonDbEntity?> GetByGroupAsync(string groupName, CancellationToken ct = default);
    Task<IReadOnlyList<MobSummonEntryDbEntity>> GetEntriesAsync(string groupName, CancellationToken ct = default);
}

/// <summary>DB-8b: attendance event catalog (rAthena db/attendance.yml).</summary>
public interface IAttendanceCatalogDbRepository
{
    Task<IReadOnlyList<AttendanceCatalogDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceCatalogRewardDbEntity>> GetRewardsAsync(int attendanceId, CancellationToken ct = default);
}

/// <summary>DB-8b: cash shop tabs + per-tab items (rAthena item_cash.yml).</summary>
public interface IItemCashDbRepository
{
    Task<IReadOnlyList<ItemCashDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemCashEntryDbEntity>> GetEntriesAsync(string tab, CancellationToken ct = default);
}

/// <summary>DB-8b: item-group random bags (rAthena item_group_db.yml).</summary>
public interface IItemGroupCatalogDbRepository
{
    Task<IReadOnlyList<ItemGroupCatalogDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemGroupCatalogEntryDbEntity>> GetEntriesAsync(string groupName, CancellationToken ct = default);
}

/// <summary>DB-8b: gift-box/package contents (rAthena item_packages.yml).</summary>
public interface IItemPackageDbRepository
{
    Task<IReadOnlyList<ItemPackageDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemPackageEntryDbEntity>> GetEntriesAsync(string itemAegis, CancellationToken ct = default);
}

/// <summary>DB-8b: equipment-combo bonuses (rAthena item_combos.yml).</summary>
public interface IItemComboDbRepository
{
    Task<IReadOnlyList<ItemComboDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemComboMemberDbEntity>> GetMembersAsync(int comboId, CancellationToken ct = default);
    /// <summary>All combos that include the given item — used by the EquipBonus calc.</summary>
    Task<IReadOnlyList<ItemComboMemberDbEntity>> GetCombosForItemAsync(string itemAegis, CancellationToken ct = default);
}
