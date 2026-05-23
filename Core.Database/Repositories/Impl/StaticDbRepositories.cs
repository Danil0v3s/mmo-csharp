using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class AbraDbRepository(GameDbContext ctx) : IAbraDbRepository
{
    public async Task<IReadOnlyList<AbraDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AbraDb.AsNoTracking().ToListAsync(ct);
}

internal sealed class MagicMushroomDbRepository(GameDbContext ctx) : IMagicMushroomDbRepository
{
    public async Task<IReadOnlyList<MagicMushroomDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MagicMushroomDb.AsNoTracking().ToListAsync(ct);
}

internal sealed class SpellbookDbRepository(GameDbContext ctx) : ISpellbookDbRepository
{
    public async Task<IReadOnlyList<SpellbookDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.SpellbookDb.AsNoTracking().ToListAsync(ct);
    public async Task<SpellbookDbEntity?> GetByBookAsync(string bookNameAegis, CancellationToken ct = default)
        => await ctx.SpellbookDb.AsNoTracking().FirstOrDefaultAsync(s => s.BookNameAegis == bookNameAegis, ct);
}

internal sealed class QuestDbRepository(GameDbContext ctx) : IQuestDbRepository
{
    public async Task<IReadOnlyList<QuestDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.QuestDb.AsNoTracking().ToListAsync(ct);
    public async Task<QuestDbEntity?> GetByIdAsync(uint questId, CancellationToken ct = default)
        => await ctx.QuestDb.AsNoTracking().FirstOrDefaultAsync(q => q.QuestId == questId, ct);
}

internal sealed class PetDbRepository(GameDbContext ctx) : IPetDbRepository
{
    public async Task<IReadOnlyList<PetDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.PetDb.AsNoTracking().ToListAsync(ct);
    public async Task<PetDbEntity?> GetByMobAsync(string mobAegis, CancellationToken ct = default)
        => await ctx.PetDb.AsNoTracking().FirstOrDefaultAsync(p => p.MobAegis == mobAegis, ct);
}

internal sealed class AchievementDbRepository(GameDbContext ctx) : IAchievementDbRepository
{
    public async Task<IReadOnlyList<AchievementDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AchievementDb.AsNoTracking().ToListAsync(ct);
    public async Task<AchievementDbEntity?> GetByIdAsync(uint achievementId, CancellationToken ct = default)
        => await ctx.AchievementDb.AsNoTracking().FirstOrDefaultAsync(a => a.AchievementId == achievementId, ct);
}

internal sealed class HomunculusDbRepository(GameDbContext ctx) : IHomunculusDbRepository
{
    public async Task<IReadOnlyList<HomunculusDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.HomunculusDb.AsNoTracking().ToListAsync(ct);
    public async Task<HomunculusDbEntity?> GetByClassAsync(string classAegis, CancellationToken ct = default)
        => await ctx.HomunculusDb.AsNoTracking().FirstOrDefaultAsync(h => h.ClassAegis == classAegis, ct);
}

internal sealed class MercenaryDbRepository(GameDbContext ctx) : IMercenaryDbRepository
{
    public async Task<IReadOnlyList<MercenaryDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MercenaryDb.AsNoTracking().ToListAsync(ct);
    public async Task<MercenaryDbEntity?> GetByIdAsync(uint mercId, CancellationToken ct = default)
        => await ctx.MercenaryDb.AsNoTracking().FirstOrDefaultAsync(m => m.MercId == mercId, ct);
}

internal sealed class InstanceDbRepository(GameDbContext ctx) : IInstanceDbRepository
{
    public async Task<IReadOnlyList<InstanceDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.InstanceDb.AsNoTracking().ToListAsync(ct);
    public async Task<InstanceDbEntity?> GetByIdAsync(uint instanceId, CancellationToken ct = default)
        => await ctx.InstanceDb.AsNoTracking().FirstOrDefaultAsync(i => i.InstanceId == instanceId, ct);
}

internal sealed class MercenarySkillDbRepository(GameDbContext ctx) : IMercenarySkillDbRepository
{
    public async Task<IReadOnlyList<MercenarySkillDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MercenarySkillDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<MercenarySkillDbEntity>> GetByMercAsync(uint mercId, CancellationToken ct = default)
        => await ctx.MercenarySkillDb.AsNoTracking().Where(s => s.MercId == mercId).ToListAsync(ct);
}

internal sealed class HomunculusSkillTreeDbRepository(GameDbContext ctx) : IHomunculusSkillTreeDbRepository
{
    public async Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.HomunculusSkillTreeDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetByClassAsync(string classAegis, CancellationToken ct = default)
        => await ctx.HomunculusSkillTreeDb.AsNoTracking().Where(s => s.ClassAegis == classAegis).ToListAsync(ct);
}

// Battleground catalog: read via existing JSON-payload table; DB-8 wires
// the deserializer. No new typed repo here.

internal sealed class StylistDbRepository(GameDbContext ctx) : IStylistDbRepository
{
    public async Task<IReadOnlyList<StylistDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.StylistDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<StylistDbEntity>> GetByLookAsync(int look, CancellationToken ct = default)
        => await ctx.StylistDb.AsNoTracking().Where(s => s.Look == look).ToListAsync(ct);
}

internal sealed class AchievementLevelDbRepository(GameDbContext ctx) : IAchievementLevelDbRepository
{
    public async Task<IReadOnlyList<AchievementLevelDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AchievementLevelDb.AsNoTracking().OrderBy(l => l.Level).ToListAsync(ct);
}

internal sealed class JobAspdDbRepository(GameDbContext ctx) : IJobAspdDbRepository
{
    public async Task<IReadOnlyList<JobAspdDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.JobAspdDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<JobAspdDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobAspdDb.AsNoTracking().Where(j => j.JobAegis == jobAegis).ToListAsync(ct);
}

internal sealed class ConstDbRepository(GameDbContext ctx) : IConstDbRepository
{
    public async Task<IReadOnlyList<ConstDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ConstDb.AsNoTracking().ToListAsync(ct);
    public async Task<ConstDbEntity?> GetByNameAsync(string name, CancellationToken ct = default)
        => await ctx.ConstDb.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name, ct);
}

// DB-8a — re-normalized catalogs

internal sealed class LevelPenaltyDbRepository(GameDbContext ctx) : ILevelPenaltyDbRepository
{
    public async Task<IReadOnlyList<LevelPenaltyDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.LevelPenaltyDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<LevelPenaltyDifferenceDbEntity>> GetDifferencesAsync(string penaltyType, CancellationToken ct = default)
        => await ctx.LevelPenaltyDifferenceDb.AsNoTracking().Where(d => d.PenaltyType == penaltyType).OrderBy(d => d.Difference).ToListAsync(ct);
    public async Task<IReadOnlyList<LevelPenaltyDifferenceDbEntity>> GetAllDifferencesAsync(CancellationToken ct = default)
        => await ctx.LevelPenaltyDifferenceDb.AsNoTracking().OrderBy(d => d.PenaltyType).ThenBy(d => d.Difference).ToListAsync(ct);
}

internal sealed class AttrFixDbRepository(GameDbContext ctx) : IAttrFixDbRepository
{
    public async Task<IReadOnlyList<AttrFixDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AttrFixDb.AsNoTracking().ToListAsync(ct);
    public async Task<int> GetMultiplierAsync(int level, string attackerElement, string defenderElement, CancellationToken ct = default)
    {
        var row = await ctx.AttrFixDb.AsNoTracking().FirstOrDefaultAsync(
            r => r.Level == level && r.AttackerElement == attackerElement && r.DefenderElement == defenderElement, ct);
        return row?.Multiplier ?? 100; // default neutral
    }
}

internal sealed class ReputationGroupDbRepository(GameDbContext ctx) : IReputationGroupDbRepository
{
    public async Task<IReadOnlyList<ReputationGroupDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ReputationGroupDb.AsNoTracking().ToListAsync(ct);
    public async Task<ReputationGroupDbEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await ctx.ReputationGroupDb.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
    public async Task<IReadOnlyList<ReputationGroupMemberDbEntity>> GetMembersAsync(int groupId, CancellationToken ct = default)
        => await ctx.ReputationGroupMemberDb.AsNoTracking().Where(m => m.GroupId == groupId).ToListAsync(ct);
}

// DB-8b — tier-2 single-child re-normalized catalog repos

internal sealed class MobSummonDbRepository(GameDbContext ctx) : IMobSummonDbRepository
{
    public async Task<IReadOnlyList<MobSummonDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MobSummonDb.AsNoTracking().ToListAsync(ct);
    public async Task<MobSummonDbEntity?> GetByGroupAsync(string groupName, CancellationToken ct = default)
        => await ctx.MobSummonDb.AsNoTracking().FirstOrDefaultAsync(g => g.GroupName == groupName, ct);
    public async Task<IReadOnlyList<MobSummonEntryDbEntity>> GetEntriesAsync(string groupName, CancellationToken ct = default)
        => await ctx.MobSummonEntryDb.AsNoTracking().Where(e => e.GroupName == groupName).ToListAsync(ct);
}

internal sealed class AttendanceCatalogDbRepository(GameDbContext ctx) : IAttendanceCatalogDbRepository
{
    public async Task<IReadOnlyList<AttendanceCatalogDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AttendanceCatalogDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<AttendanceCatalogRewardDbEntity>> GetRewardsAsync(int attendanceId, CancellationToken ct = default)
        => await ctx.AttendanceCatalogRewardDb.AsNoTracking().Where(r => r.AttendanceId == attendanceId).OrderBy(r => r.Day).ToListAsync(ct);
}

internal sealed class ItemCashDbRepository(GameDbContext ctx) : IItemCashDbRepository
{
    public async Task<IReadOnlyList<ItemCashDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemCashDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemCashEntryDbEntity>> GetEntriesAsync(string tab, CancellationToken ct = default)
        => await ctx.ItemCashEntryDb.AsNoTracking().Where(e => e.Tab == tab).ToListAsync(ct);
}

internal sealed class ItemGroupCatalogDbRepository(GameDbContext ctx) : IItemGroupCatalogDbRepository
{
    public async Task<IReadOnlyList<ItemGroupCatalogDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemGroupCatalogDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemGroupCatalogEntryDbEntity>> GetEntriesAsync(string groupName, CancellationToken ct = default)
        => await ctx.ItemGroupCatalogEntryDb.AsNoTracking().Where(e => e.GroupName == groupName).OrderBy(e => e.SubGroup).ThenBy(e => e.Index).ToListAsync(ct);
}

internal sealed class ItemPackageDbRepository(GameDbContext ctx) : IItemPackageDbRepository
{
    public async Task<IReadOnlyList<ItemPackageDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemPackageDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemPackageEntryDbEntity>> GetEntriesAsync(string itemAegis, CancellationToken ct = default)
        => await ctx.ItemPackageEntryDb.AsNoTracking().Where(e => e.ItemAegis == itemAegis).OrderBy(e => e.GroupId).ToListAsync(ct);
}

internal sealed class ItemComboDbRepository(GameDbContext ctx) : IItemComboDbRepository
{
    public async Task<IReadOnlyList<ItemComboDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemComboDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemComboMemberDbEntity>> GetMembersAsync(int comboId, CancellationToken ct = default)
        => await ctx.ItemComboMemberDb.AsNoTracking().Where(m => m.ComboId == comboId).ToListAsync(ct);
    public async Task<IReadOnlyList<ItemComboMemberDbEntity>> GetCombosForItemAsync(string itemAegis, CancellationToken ct = default)
        => await ctx.ItemComboMemberDb.AsNoTracking().Where(m => m.MemberItemAegis == itemAegis).ToListAsync(ct);
    public async Task<IReadOnlyList<ItemComboMemberDbEntity>> GetAllMembersAsync(CancellationToken ct = default)
        => await ctx.ItemComboMemberDb.AsNoTracking().ToListAsync(ct);
}

// DB-8c — skill tree repos

internal sealed class SkillTreeDbRepository(GameDbContext ctx) : ISkillTreeDbRepository
{
    public async Task<IReadOnlyList<SkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.SkillTreeDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<SkillTreeInheritDbEntity>> GetInheritsAsync(string childJobAegis, CancellationToken ct = default)
        => await ctx.SkillTreeInheritDb.AsNoTracking().Where(i => i.ChildJobAegis == childJobAegis).ToListAsync(ct);
    public async Task<IReadOnlyList<SkillTreeEntryDbEntity>> GetEntriesAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.SkillTreeEntryDb.AsNoTracking().Where(e => e.JobAegis == jobAegis).ToListAsync(ct);
    public async Task<IReadOnlyList<SkillTreeRequirementDbEntity>> GetRequirementsAsync(string jobAegis, string skillAegis, CancellationToken ct = default)
        => await ctx.SkillTreeRequirementDb.AsNoTracking().Where(r => r.JobAegis == jobAegis && r.SkillAegis == skillAegis).ToListAsync(ct);
}

internal sealed class GuildSkillTreeDbRepository(GameDbContext ctx) : IGuildSkillTreeDbRepository
{
    public async Task<IReadOnlyList<GuildSkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.GuildSkillTreeDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<GuildSkillTreeRequirementDbEntity>> GetRequirementsAsync(string skillAegis, CancellationToken ct = default)
        => await ctx.GuildSkillTreeRequirementDb.AsNoTracking().Where(r => r.SkillAegis == skillAegis).ToListAsync(ct);
}

// DB-8d — job table repos

internal sealed class JobInfoDbRepository(GameDbContext ctx) : IJobInfoDbRepository
{
    public async Task<IReadOnlyList<JobInfoDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.JobInfoDb.AsNoTracking().ToListAsync(ct);
    public async Task<JobInfoDbEntity?> GetByJobAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobInfoDb.AsNoTracking().FirstOrDefaultAsync(j => j.JobAegis == jobAegis, ct);
    public async Task<IReadOnlyList<JobBonusStatsDbEntity>> GetBonusStatsAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobBonusStatsDb.AsNoTracking().Where(b => b.JobAegis == jobAegis).OrderBy(b => b.Level).ToListAsync(ct);
}

internal sealed class JobExpDbRepository(GameDbContext ctx) : IJobExpDbRepository
{
    public async Task<IReadOnlyList<JobExpDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobExpDb.AsNoTracking().Where(e => e.JobAegis == jobAegis).OrderBy(e => e.Level).ToListAsync(ct);
    public async Task<JobMaxLevelDbEntity?> GetMaxLevelAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobMaxLevelDb.AsNoTracking().FirstOrDefaultAsync(m => m.JobAegis == jobAegis, ct);
}

internal sealed class JobBasePointsDbRepository(GameDbContext ctx) : IJobBasePointsDbRepository
{
    public async Task<IReadOnlyList<JobBasePointsDbEntity>> GetByJobAsync(string jobAegis, CancellationToken ct = default)
        => await ctx.JobBasePointsDb.AsNoTracking().Where(p => p.JobAegis == jobAegis).OrderBy(p => p.Level).ToListAsync(ct);
}

// DB-8e — status.yml repo

internal sealed class StatusDbRepository(GameDbContext ctx) : IStatusDbRepository
{
    public async Task<IReadOnlyList<StatusDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.StatusDb.AsNoTracking().ToListAsync(ct);
    public async Task<StatusDbEntity?> GetByNameAsync(string statusName, CancellationToken ct = default)
        => await ctx.StatusDb.AsNoTracking().FirstOrDefaultAsync(s => s.StatusName == statusName, ct);
    public async Task<IReadOnlyList<StatusDbFlagEntity>> GetAllFlagsAsync(string statusName, CancellationToken ct = default)
        => await ctx.StatusDbFlag.AsNoTracking().Where(f => f.StatusName == statusName).ToListAsync(ct);
    public async Task<IReadOnlyList<StatusDbFlagEntity>> GetAllFlagsAsync(CancellationToken ct = default)
        => await ctx.StatusDbFlag.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<StatusDbFlagEntity>> GetFlagsByCategoryAsync(string statusName, string category, CancellationToken ct = default)
        => await ctx.StatusDbFlag.AsNoTracking().Where(f => f.StatusName == statusName && f.Category == category).ToListAsync(ct);
}

// DB-8f — battleground + elemental repos

internal sealed class BattlegroundCatalogDbRepository(GameDbContext ctx) : IBattlegroundCatalogDbRepository
{
    public async Task<IReadOnlyList<BattlegroundTypeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.BattlegroundTypeDb.AsNoTracking().ToListAsync(ct);
    public async Task<BattlegroundTypeDbEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await ctx.BattlegroundTypeDb.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
    public async Task<IReadOnlyList<BattlegroundJobRestrictionDbEntity>> GetJobRestrictionsAsync(int bgId, CancellationToken ct = default)
        => await ctx.BattlegroundJobRestrictionDb.AsNoTracking().Where(r => r.BgId == bgId).ToListAsync(ct);
    public async Task<IReadOnlyList<BattlegroundLocationDbEntity>> GetLocationsAsync(int bgId, CancellationToken ct = default)
        => await ctx.BattlegroundLocationDb.AsNoTracking().Where(l => l.BgId == bgId).ToListAsync(ct);
    public async Task<IReadOnlyList<BattlegroundLocationDbEntity>> GetAllLocationsAsync(CancellationToken ct = default)
        => await ctx.BattlegroundLocationDb.AsNoTracking().ToListAsync(ct);
}

internal sealed class ElementalCatalogDbRepository(GameDbContext ctx) : IElementalCatalogDbRepository
{
    public async Task<IReadOnlyList<ElementalCatalogDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ElementalCatalogDb.AsNoTracking().ToListAsync(ct);
    public async Task<ElementalCatalogDbEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await ctx.ElementalCatalogDb.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task<IReadOnlyList<ElementalModeDbEntity>> GetModesAsync(int elementalId, CancellationToken ct = default)
        => await ctx.ElementalModeDb.AsNoTracking().Where(m => m.ElementalId == elementalId).ToListAsync(ct);
}

// DB-8g — enchant pipeline repos

internal sealed class ItemEnchantDbRepository(GameDbContext ctx) : IItemEnchantDbRepository
{
    public async Task<IReadOnlyList<ItemEnchantDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemEnchantDb.AsNoTracking().ToListAsync(ct);
    public async Task<ItemEnchantDbEntity?> GetByIdAsync(int enchantId, CancellationToken ct = default)
        => await ctx.ItemEnchantDb.AsNoTracking().FirstOrDefaultAsync(e => e.EnchantId == enchantId, ct);
    public async Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetAllTargetsAsync(CancellationToken ct = default)
        => await ctx.ItemEnchantTargetDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetTargetsAsync(int enchantId, CancellationToken ct = default)
        => await ctx.ItemEnchantTargetDb.AsNoTracking().Where(t => t.EnchantId == enchantId).ToListAsync(ct);
    public async Task<IReadOnlyList<ItemEnchantMaterialDbEntity>> GetMaterialsAsync(int enchantId, CancellationToken ct = default)
        => await ctx.ItemEnchantMaterialDb.AsNoTracking().Where(m => m.EnchantId == enchantId).ToListAsync(ct);
    public async Task<IReadOnlyList<ItemEnchantSlotDbEntity>> GetSlotsAsync(int enchantId, CancellationToken ct = default)
        => await ctx.ItemEnchantSlotDb.AsNoTracking().Where(s => s.EnchantId == enchantId).OrderBy(s => s.Slot).ToListAsync(ct);
    public async Task<IReadOnlyList<ItemEnchantOptionDbEntity>> GetOptionsAsync(int enchantId, CancellationToken ct = default)
        => await ctx.ItemEnchantOptionDb.AsNoTracking().Where(o => o.EnchantId == enchantId).ToListAsync(ct);
}

internal sealed class ItemReformDbRepository(GameDbContext ctx) : IItemReformDbRepository
{
    public async Task<IReadOnlyList<ItemReformDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemReformDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemReformBaseDbEntity>> GetAllBasesAsync(CancellationToken ct = default)
        => await ctx.ItemReformBaseDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<ItemReformBaseDbEntity>> GetBasesAsync(string resultItemAegis, CancellationToken ct = default)
        => await ctx.ItemReformBaseDb.AsNoTracking().Where(b => b.ResultItemAegis == resultItemAegis).ToListAsync(ct);
}

internal sealed class LaphineSynthesisDbRepository(GameDbContext ctx) : ILaphineSynthesisDbRepository
{
    public async Task<IReadOnlyList<LaphineSynthesisDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.LaphineSynthesisDb.AsNoTracking().ToListAsync(ct);
    public async Task<LaphineSynthesisDbEntity?> GetByRecipeAsync(string recipeItem, CancellationToken ct = default)
        => await ctx.LaphineSynthesisDb.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeItem == recipeItem, ct);
    public async Task<IReadOnlyList<LaphineSynthesisRequirementDbEntity>> GetRequirementsAsync(string recipeItem, CancellationToken ct = default)
        => await ctx.LaphineSynthesisRequirementDb.AsNoTracking().Where(r => r.RecipeItem == recipeItem).ToListAsync(ct);
}

internal sealed class LaphineUpgradeDbRepository(GameDbContext ctx) : ILaphineUpgradeDbRepository
{
    public async Task<IReadOnlyList<LaphineUpgradeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.LaphineUpgradeDb.AsNoTracking().ToListAsync(ct);
    public async Task<LaphineUpgradeDbEntity?> GetByUpgradeAsync(string upgradeItem, CancellationToken ct = default)
        => await ctx.LaphineUpgradeDb.AsNoTracking().FirstOrDefaultAsync(u => u.UpgradeItem == upgradeItem, ct);
    public async Task<IReadOnlyList<LaphineUpgradeTargetDbEntity>> GetTargetsAsync(string upgradeItem, CancellationToken ct = default)
        => await ctx.LaphineUpgradeTargetDb.AsNoTracking().Where(t => t.UpgradeItem == upgradeItem).ToListAsync(ct);
}

internal sealed class ItemRandomOptGroupDbRepository(GameDbContext ctx) : IItemRandomOptGroupDbRepository
{
    public async Task<IReadOnlyList<ItemRandomOptGroupDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.ItemRandomOptGroupDb.AsNoTracking().ToListAsync(ct);
    public async Task<ItemRandomOptGroupDbEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await ctx.ItemRandomOptGroupDb.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
    public async Task<ItemRandomOptGroupDbEntity?> GetByNameAsync(string groupName, CancellationToken ct = default)
        => await ctx.ItemRandomOptGroupDb.AsNoTracking().FirstOrDefaultAsync(g => g.GroupName == groupName, ct);
    public async Task<IReadOnlyList<ItemRandomOptGroupOptionDbEntity>> GetOptionsAsync(int groupId, CancellationToken ct = default)
        => await ctx.ItemRandomOptGroupOptionDb.AsNoTracking().Where(o => o.GroupId == groupId).ToListAsync(ct);
}

// DB-8h — refine + enchantgrade repos

internal sealed class RefineDbRepository(GameDbContext ctx) : IRefineDbRepository
{
    public async Task<IReadOnlyList<RefineGroupDbEntity>> GetAllGroupsAsync(CancellationToken ct = default)
        => await ctx.RefineGroupDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<RefineLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default)
        => await ctx.RefineLevelDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<RefineChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default)
        => await ctx.RefineChanceDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<RefineLevelDbEntity>> GetLevelsForGroupAsync(string groupName, CancellationToken ct = default)
        => await ctx.RefineLevelDb.AsNoTracking().Where(l => l.GroupName == groupName).ToListAsync(ct);
    public async Task<IReadOnlyList<RefineChanceDbEntity>> GetChancesForGroupAsync(string groupName, CancellationToken ct = default)
        => await ctx.RefineChanceDb.AsNoTracking().Where(c => c.GroupName == groupName).ToListAsync(ct);
}

internal sealed class EnchantGradeDbRepository(GameDbContext ctx) : IEnchantGradeDbRepository
{
    public async Task<IReadOnlyList<EnchantGradeDbEntity>> GetAllGroupsAsync(CancellationToken ct = default)
        => await ctx.EnchantGradeDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<EnchantGradeLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default)
        => await ctx.EnchantGradeLevelDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<EnchantGradeChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default)
        => await ctx.EnchantGradeChanceDb.AsNoTracking().ToListAsync(ct);
}

// DB-8i — drop override repos

internal sealed class MapDropDbRepository(GameDbContext ctx) : IMapDropDbRepository
{
    public async Task<IReadOnlyList<MapDropDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MapDropDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<MapDropEntryDbEntity>> GetEntriesAsync(string mapName, CancellationToken ct = default)
        => await ctx.MapDropEntryDb.AsNoTracking().Where(e => e.MapName == mapName).OrderBy(e => e.EntryIndex).ToListAsync(ct);
    public async Task<IReadOnlyList<MapDropEntryDbEntity>> GetAllEntriesAsync(CancellationToken ct = default)
        => await ctx.MapDropEntryDb.AsNoTracking().OrderBy(e => e.MapName).ThenBy(e => e.EntryIndex).ToListAsync(ct);
}

internal sealed class MobItemRatioDbRepository(GameDbContext ctx) : IMobItemRatioDbRepository
{
    public async Task<IReadOnlyList<MobItemRatioDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MobItemRatioDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<MobItemRatioMobDbEntity>> GetMobFiltersAsync(string itemAegis, CancellationToken ct = default)
        => await ctx.MobItemRatioMobDb.AsNoTracking().Where(m => m.ItemAegis == itemAegis).ToListAsync(ct);
    public async Task<IReadOnlyList<MobItemRatioMobDbEntity>> GetAllMobFiltersAsync(CancellationToken ct = default)
        => await ctx.MobItemRatioMobDb.AsNoTracking().ToListAsync(ct);
}
