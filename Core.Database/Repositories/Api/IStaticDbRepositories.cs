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

// ============================================================================
// DB-8c: skill tree repos
// ============================================================================

/// <summary>DB-8c: per-job skill tree (rAthena skill_tree.yml).</summary>
public interface ISkillTreeDbRepository
{
    Task<IReadOnlyList<SkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillTreeInheritDbEntity>> GetInheritsAsync(string childJobAegis, CancellationToken ct = default);
    Task<IReadOnlyList<SkillTreeEntryDbEntity>> GetEntriesAsync(string jobAegis, CancellationToken ct = default);
    Task<IReadOnlyList<SkillTreeRequirementDbEntity>> GetRequirementsAsync(string jobAegis, string skillAegis, CancellationToken ct = default);
}

/// <summary>DB-8c: guild skill tree (rAthena guild_skill_tree.yml).</summary>
public interface IGuildSkillTreeDbRepository
{
    Task<IReadOnlyList<GuildSkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GuildSkillTreeRequirementDbEntity>> GetRequirementsAsync(string skillAegis, CancellationToken ct = default);
}

// ============================================================================
// DB-8d: job table repos
// ============================================================================

/// <summary>DB-8d: per-job stat constants (rAthena job_stats.yml).</summary>
public interface IJobInfoDbRepository
{
    Task<IReadOnlyList<JobInfoDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<JobInfoDbEntity?> GetByJobAsync(string jobAegis, CancellationToken ct = default);
    Task<IReadOnlyList<JobBonusStatsDbEntity>> GetBonusStatsAsync(string jobAegis, CancellationToken ct = default);
}

/// <summary>DB-8d: per-job per-level EXP curves (rAthena job_exp.yml).</summary>
public interface IJobExpDbRepository
{
    Task<IReadOnlyList<JobExpDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default);
    Task<JobMaxLevelDbEntity?> GetMaxLevelAsync(string jobAegis, CancellationToken ct = default);
}

/// <summary>DB-8d: per-job per-level HP/SP/AP base values (rAthena job_basepoints.yml).</summary>
public interface IJobBasePointsDbRepository
{
    Task<IReadOnlyList<JobBasePointsDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default);
}

// ============================================================================
// DB-8e: status.yml repo
// ============================================================================

/// <summary>DB-8e: per-SC catalog + nested flag maps (rAthena status.yml, ~440 SCs).</summary>
public interface IStatusDbRepository
{
    Task<IReadOnlyList<StatusDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<StatusDbEntity?> GetByNameAsync(string statusName, CancellationToken ct = default);
    /// <summary>All flag rows for an SC, regardless of category.</summary>
    Task<IReadOnlyList<StatusDbFlagEntity>> GetAllFlagsAsync(string statusName, CancellationToken ct = default);
    /// <summary>Flag rows for an SC in one category (State / CalcFlag / Flag / Fail / EndOnStart / EndOnEnd / EndOnRestart).</summary>
    Task<IReadOnlyList<StatusDbFlagEntity>> GetFlagsByCategoryAsync(string statusName, string category, CancellationToken ct = default);
}

// ============================================================================
// DB-8f: battleground + elemental repos
// ============================================================================

/// <summary>DB-8f: battleground type catalog (rAthena battleground_db.yml).</summary>
public interface IBattlegroundCatalogDbRepository
{
    Task<IReadOnlyList<BattlegroundTypeDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<BattlegroundTypeDbEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BattlegroundJobRestrictionDbEntity>> GetJobRestrictionsAsync(int bgId, CancellationToken ct = default);
    Task<IReadOnlyList<BattlegroundLocationDbEntity>> GetLocationsAsync(int bgId, CancellationToken ct = default);
    /// <summary>
    /// All BG locations across every type — single query, no bg_id filter.
    /// Used by the BG queue to build a global map-reservation pool.
    /// </summary>
    Task<IReadOnlyList<BattlegroundLocationDbEntity>> GetAllLocationsAsync(CancellationToken ct = default);
}

/// <summary>DB-8f: elemental servant catalog (rAthena elemental_db.yml).</summary>
public interface IElementalCatalogDbRepository
{
    Task<IReadOnlyList<ElementalCatalogDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ElementalCatalogDbEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ElementalModeDbEntity>> GetModesAsync(int elementalId, CancellationToken ct = default);
}

// ============================================================================
// DB-8g: enchant pipeline repos (item_enchant, item_reform, laphine, randomopt)
// ============================================================================

/// <summary>DB-8g: item enchant pipeline (rAthena item_enchant.yml, 140 pipelines).</summary>
public interface IItemEnchantDbRepository
{
    Task<IReadOnlyList<ItemEnchantDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ItemEnchantDbEntity?> GetByIdAsync(int enchantId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetAllTargetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetTargetsAsync(int enchantId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEnchantMaterialDbEntity>> GetMaterialsAsync(int enchantId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEnchantSlotDbEntity>> GetSlotsAsync(int enchantId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEnchantOptionDbEntity>> GetOptionsAsync(int enchantId, CancellationToken ct = default);
}

/// <summary>DB-8g: item reform recipes (rAthena item_reform.yml).</summary>
public interface IItemReformDbRepository
{
    Task<IReadOnlyList<ItemReformDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemReformBaseDbEntity>> GetAllBasesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemReformBaseDbEntity>> GetBasesAsync(string resultItemAegis, CancellationToken ct = default);
}

/// <summary>DB-8g: laphine synthesis recipes (rAthena laphine_synthesis.yml).</summary>
public interface ILaphineSynthesisDbRepository
{
    Task<IReadOnlyList<LaphineSynthesisDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<LaphineSynthesisDbEntity?> GetByRecipeAsync(string recipeItem, CancellationToken ct = default);
    Task<IReadOnlyList<LaphineSynthesisRequirementDbEntity>> GetRequirementsAsync(string recipeItem, CancellationToken ct = default);
}

/// <summary>DB-8g: laphine upgrade recipes (rAthena laphine_upgrade.yml).</summary>
public interface ILaphineUpgradeDbRepository
{
    Task<IReadOnlyList<LaphineUpgradeDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<LaphineUpgradeDbEntity?> GetByUpgradeAsync(string upgradeItem, CancellationToken ct = default);
    Task<IReadOnlyList<LaphineUpgradeTargetDbEntity>> GetTargetsAsync(string upgradeItem, CancellationToken ct = default);
}

/// <summary>DB-8g: item random-opt groups (rAthena item_randomopt_group.yml).</summary>
public interface IItemRandomOptGroupDbRepository
{
    Task<IReadOnlyList<ItemRandomOptGroupDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ItemRandomOptGroupDbEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ItemRandomOptGroupDbEntity?> GetByNameAsync(string groupName, CancellationToken ct = default);
    Task<IReadOnlyList<ItemRandomOptGroupOptionDbEntity>> GetOptionsAsync(int groupId, CancellationToken ct = default);
}

// ============================================================================
// DB-8h: refine + enchantgrade repos
// ============================================================================

/// <summary>DB-8h: refine catalog (rAthena refine.yml — groups + levels + chances).</summary>
public interface IRefineDbRepository
{
    Task<IReadOnlyList<RefineGroupDbEntity>> GetAllGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RefineLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RefineChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RefineLevelDbEntity>> GetLevelsForGroupAsync(string groupName, CancellationToken ct = default);
    Task<IReadOnlyList<RefineChanceDbEntity>> GetChancesForGroupAsync(string groupName, CancellationToken ct = default);
}

/// <summary>DB-8h: enchantgrade catalog (rAthena enchantgrade.yml — groups + levels + chances).</summary>
public interface IEnchantGradeDbRepository
{
    Task<IReadOnlyList<EnchantGradeDbEntity>> GetAllGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnchantGradeLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnchantGradeChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default);
}

// ============================================================================
// DB-8i: drop override repos
// ============================================================================

/// <summary>
/// DB-8i: per-map drop overrides (rAthena <c>db/map_drops.yml</c>).
/// Each map row carries N <see cref="MapDropEntryDbEntity"/> children;
/// the consumer rolls these AFTER the mob's own drop table on death.
/// </summary>
public interface IMapDropDbRepository
{
    Task<IReadOnlyList<MapDropDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MapDropEntryDbEntity>> GetEntriesAsync(string mapName, CancellationToken ct = default);
    /// <summary>All entries across every map — single query, used at boot to fill the in-memory cache.</summary>
    Task<IReadOnlyList<MapDropEntryDbEntity>> GetAllEntriesAsync(CancellationToken ct = default);
}

/// <summary>
/// DB-8i: per-item drop-rate modifier (rAthena <c>db/mob_item_ratio.yml</c>).
/// Optional mob filter restricts the multiplier to a subset of monsters
/// (no mob rows = applies to every monster that drops the item).
/// </summary>
public interface IMobItemRatioDbRepository
{
    Task<IReadOnlyList<MobItemRatioDbEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MobItemRatioMobDbEntity>> GetMobFiltersAsync(string itemAegis, CancellationToken ct = default);
    /// <summary>All mob-filter rows across every item — used at boot to fill the in-memory cache.</summary>
    Task<IReadOnlyList<MobItemRatioMobDbEntity>> GetAllMobFiltersAsync(CancellationToken ct = default);
}
