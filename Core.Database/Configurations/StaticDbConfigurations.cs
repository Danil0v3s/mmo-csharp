using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

// Bundled configurations for the read-only `_db` catalog tables
// seeded from rAthena YAML. Each one is a thin table — keeping the
// configs together avoids one-class-per-file boilerplate.

public class AbraDbEntityConfiguration : IEntityTypeConfiguration<AbraDbEntity>
{
    public void Configure(EntityTypeBuilder<AbraDbEntity> b)
    {
        b.ToTable("abra_db");
        b.HasKey(e => e.SkillName);
        b.Property(e => e.SkillName).HasColumnName("skill_name").HasMaxLength(64).IsRequired();
    }
}

public class MagicMushroomDbEntityConfiguration : IEntityTypeConfiguration<MagicMushroomDbEntity>
{
    public void Configure(EntityTypeBuilder<MagicMushroomDbEntity> b)
    {
        b.ToTable("magicmushroom_db");
        b.HasKey(e => e.SkillName);
        b.Property(e => e.SkillName).HasColumnName("skill_name").HasMaxLength(64).IsRequired();
    }
}

public class SpellbookDbEntityConfiguration : IEntityTypeConfiguration<SpellbookDbEntity>
{
    public void Configure(EntityTypeBuilder<SpellbookDbEntity> b)
    {
        b.ToTable("spellbook_db");
        b.HasKey(e => e.BookNameAegis);
        b.Property(e => e.SkillName).HasColumnName("skill_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.BookNameAegis).HasColumnName("book_name_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.PreservePoints).HasColumnName("preserve_points");
    }
}

public class QuestDbEntityConfiguration : IEntityTypeConfiguration<QuestDbEntity>
{
    public void Configure(EntityTypeBuilder<QuestDbEntity> b)
    {
        b.ToTable("quest_db");
        b.HasKey(e => e.QuestId);
        b.Property(e => e.QuestId).HasColumnName("quest_id");
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(255);
        b.Property(e => e.TimeLimit).HasColumnName("time_limit").HasMaxLength(64);
        b.Property(e => e.Mob1).HasColumnName("mob1").HasMaxLength(64);
        b.Property(e => e.Count1).HasColumnName("count1");
        b.Property(e => e.Mob2).HasColumnName("mob2").HasMaxLength(64);
        b.Property(e => e.Count2).HasColumnName("count2");
        b.Property(e => e.Mob3).HasColumnName("mob3").HasMaxLength(64);
        b.Property(e => e.Count3).HasColumnName("count3");
    }
}

public class PetDbEntityConfiguration : IEntityTypeConfiguration<PetDbEntity>
{
    public void Configure(EntityTypeBuilder<PetDbEntity> b)
    {
        b.ToTable("pet_db");
        b.HasKey(e => e.MobAegis);
        b.Property(e => e.MobAegis).HasColumnName("mob_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.TameItem).HasColumnName("tame_item").HasMaxLength(64);
        b.Property(e => e.EggItem).HasColumnName("egg_item").HasMaxLength(64);
        b.Property(e => e.EquipItem).HasColumnName("equip_item").HasMaxLength(64);
        b.Property(e => e.FoodItem).HasColumnName("food_item").HasMaxLength(64);
        b.Property(e => e.Fullness).HasColumnName("fullness");
        b.Property(e => e.HungerDelay).HasColumnName("hunger_delay");
        b.Property(e => e.IntimacyStart).HasColumnName("intimacy_start");
        b.Property(e => e.IntimacyFed).HasColumnName("intimacy_fed");
        b.Property(e => e.IntimacyOverfed).HasColumnName("intimacy_overfed");
        b.Property(e => e.IntimacyHungry).HasColumnName("intimacy_hungry");
        b.Property(e => e.IntimacyOwnerDie).HasColumnName("intimacy_owner_die");
        b.Property(e => e.CaptureRate).HasColumnName("capture_rate");
        b.Property(e => e.SpecialPerformance).HasColumnName("special_performance");
        b.Property(e => e.AttackRate).HasColumnName("attack_rate");
        b.Property(e => e.RetaliateRate).HasColumnName("retaliate_rate");
        b.Property(e => e.ChangeTargetRate).HasColumnName("change_target_rate");
        b.Property(e => e.AllowAutoFeed).HasColumnName("allow_auto_feed");
        b.Property(e => e.Script).HasColumnName("script").HasColumnType("text");
        b.Property(e => e.SupportScript).HasColumnName("support_script").HasColumnType("text");
    }
}

public class AchievementDbEntityConfiguration : IEntityTypeConfiguration<AchievementDbEntity>
{
    public void Configure(EntityTypeBuilder<AchievementDbEntity> b)
    {
        b.ToTable("achievement_db");
        b.HasKey(e => e.AchievementId);
        b.Property(e => e.AchievementId).HasColumnName("achievement_id");
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64);
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
        b.Property(e => e.Score).HasColumnName("score");
        b.Property(e => e.Dependents).HasColumnName("dependents").HasMaxLength(255);
        b.Property(e => e.Targets).HasColumnName("targets").HasColumnType("text");
    }
}

public class HomunculusDbEntityConfiguration : IEntityTypeConfiguration<HomunculusDbEntity>
{
    public void Configure(EntityTypeBuilder<HomunculusDbEntity> b)
    {
        b.ToTable("homunculus_db");
        b.HasKey(e => e.ClassAegis);
        b.Property(e => e.ClassAegis).HasColumnName("class_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        b.Property(e => e.FoodItem).HasColumnName("food_item").HasMaxLength(64);
        b.Property(e => e.HungryDelay).HasColumnName("hungry_delay");
        b.Property(e => e.Size).HasColumnName("size").HasMaxLength(24);
        b.Property(e => e.Race).HasColumnName("race").HasMaxLength(24);
        b.Property(e => e.Element).HasColumnName("element").HasMaxLength(24);
        b.Property(e => e.EleLevel).HasColumnName("ele_level");
        b.Property(e => e.AttackRange).HasColumnName("attack_range");
        b.Property(e => e.EvolutionClass).HasColumnName("evolution_class").HasMaxLength(64);
    }
}

public class MercenaryDbEntityConfiguration : IEntityTypeConfiguration<MercenaryDbEntity>
{
    public void Configure(EntityTypeBuilder<MercenaryDbEntity> b)
    {
        b.ToTable("mercenary_db");
        b.HasKey(e => e.MercId);
        b.Property(e => e.MercId).HasColumnName("merc_id");
        b.Property(e => e.AegisName).HasColumnName("aegis_name").HasMaxLength(64);
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64);
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Hp).HasColumnName("hp");
        b.Property(e => e.Sp).HasColumnName("sp");
        b.Property(e => e.Attack).HasColumnName("attack");
        b.Property(e => e.Attack2).HasColumnName("attack2");
        b.Property(e => e.Defense).HasColumnName("defense");
        b.Property(e => e.MagicDefense).HasColumnName("magic_defense");
        b.Property(e => e.Str).HasColumnName("str");
        b.Property(e => e.Agi).HasColumnName("agi");
        b.Property(e => e.Vit).HasColumnName("vit");
        b.Property(e => e.Intel).HasColumnName("intel");
        b.Property(e => e.Dex).HasColumnName("dex");
        b.Property(e => e.Luk).HasColumnName("luk");
        b.Property(e => e.AttackRange).HasColumnName("attack_range");
        b.Property(e => e.SkillRange).HasColumnName("skill_range");
        b.Property(e => e.ChaseRange).HasColumnName("chase_range");
        b.Property(e => e.Size).HasColumnName("size").HasMaxLength(24);
        b.Property(e => e.Race).HasColumnName("race").HasMaxLength(24);
        b.Property(e => e.Element).HasColumnName("element").HasMaxLength(24);
        b.Property(e => e.EleLevel).HasColumnName("ele_level");
        b.Property(e => e.WalkSpeed).HasColumnName("walk_speed");
        b.Property(e => e.AttackDelay).HasColumnName("attack_delay");
        b.Property(e => e.AttackMotion).HasColumnName("attack_motion");
        b.Property(e => e.DamageMotion).HasColumnName("damage_motion");
    }
}

public class InstanceDbEntityConfiguration : IEntityTypeConfiguration<InstanceDbEntity>
{
    public void Configure(EntityTypeBuilder<InstanceDbEntity> b)
    {
        b.ToTable("instance_db");
        b.HasKey(e => e.InstanceId);
        b.Property(e => e.InstanceId).HasColumnName("instance_id");
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64);
        b.Property(e => e.TimeLimit).HasColumnName("time_limit");
        b.Property(e => e.IdleTimeout).HasColumnName("idle_timeout");
        b.Property(e => e.EnterMap).HasColumnName("enter_map").HasMaxLength(64);
        b.Property(e => e.EnterX).HasColumnName("enter_x");
        b.Property(e => e.EnterY).HasColumnName("enter_y");
        b.Property(e => e.AdditionalMaps).HasColumnName("additional_maps").HasColumnType("text");
    }
}

/// <summary>
/// AT-F: per-merc skill grant child table. Composite key (merc_id,
/// skill_id). Empty SQL row set falls back to a baked default seed
/// in <c>MercenaryService</c>.
/// </summary>
public class MercenarySkillDbEntityConfiguration : IEntityTypeConfiguration<MercenarySkillDbEntity>
{
    public void Configure(EntityTypeBuilder<MercenarySkillDbEntity> b)
    {
        b.ToTable("mercenary_skill_db");
        b.HasKey(e => new { e.MercId, e.SkillId });
        b.Property(e => e.MercId).HasColumnName("merc_id");
        b.Property(e => e.SkillId).HasColumnName("skill_id");
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxLevel).HasColumnName("max_level");
    }
}

/// <summary>
/// AT-F: per-homunculus-class skill tree child table. Composite key
/// (class_aegis, skill_id). Empty row set falls back to baked seed in
/// <c>HomunculusService</c>.
/// </summary>
public class HomunculusSkillTreeDbEntityConfiguration : IEntityTypeConfiguration<HomunculusSkillTreeDbEntity>
{
    public void Configure(EntityTypeBuilder<HomunculusSkillTreeDbEntity> b)
    {
        b.ToTable("homunculus_skill_tree_db");
        b.HasKey(e => new { e.ClassAegis, e.SkillId });
        b.Property(e => e.ClassAegis).HasColumnName("class_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.SkillId).HasColumnName("skill_id");
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxLevel).HasColumnName("max_level");
        b.Property(e => e.RequiredLevel).HasColumnName("required_level");
        b.Property(e => e.RequiredIntimacy).HasColumnName("required_intimacy");
        b.Property(e => e.RequireEvolution).HasColumnName("require_evolution");
    }
}

// Battleground catalog: already in DB as `battleground_db` JSON
// payload (DB-5; see CatalogEntities.BattlegroundDbEntity). The
// service consumes that JSON via a typed deserializer instead of a
// duplicate child table. Wiring that consumer is task #145 DB-8.

/// <summary>
/// AT-G: stylist option catalog. Composite key (look, client_index)
/// — same row applies to Human + Doram unless <c>doram_only</c>.
/// </summary>
public class StylistDbEntityConfiguration : IEntityTypeConfiguration<StylistDbEntity>
{
    public void Configure(EntityTypeBuilder<StylistDbEntity> b)
    {
        b.ToTable("stylist_db");
        b.HasKey(e => new { e.Look, e.ClientIndex, e.DoramOnly });
        b.Property(e => e.Look).HasColumnName("look");
        b.Property(e => e.ClientIndex).HasColumnName("client_index");
        b.Property(e => e.Value).HasColumnName("value");
        b.Property(e => e.DoramOnly).HasColumnName("doram_only");
        b.Property(e => e.CostZeny).HasColumnName("cost_zeny");
        b.Property(e => e.RequiredItemAegis).HasColumnName("required_item_aegis").HasMaxLength(64);
        b.Property(e => e.RequiredItemBoxAegis).HasColumnName("required_item_box_aegis").HasMaxLength(64);
    }
}

/// <summary>
/// AT-G: achievement-level XP curve (stock yml caps at 20 levels).
/// </summary>
public class AchievementLevelDbEntityConfiguration : IEntityTypeConfiguration<AchievementLevelDbEntity>
{
    public void Configure(EntityTypeBuilder<AchievementLevelDbEntity> b)
    {
        b.ToTable("achievement_level_db");
        b.HasKey(e => e.Level);
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.RequiredPoints).HasColumnName("required_points");
    }
}

/// <summary>
/// AT-G: per-job per-weapon ASPD base delay. Composite key
/// (job_aegis, weapon_type).
/// </summary>
public class JobAspdDbEntityConfiguration : IEntityTypeConfiguration<JobAspdDbEntity>
{
    public void Configure(EntityTypeBuilder<JobAspdDbEntity> b)
    {
        b.ToTable("job_aspd_db");
        b.HasKey(e => new { e.JobAegis, e.WeaponType });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.WeaponType).HasColumnName("weapon_type");
        b.Property(e => e.BaseDelayMs).HasColumnName("base_delay_ms");
    }
}

/// <summary>
/// AT-G: script constant catalog. <c>Name</c> is the unique key the
/// script engine resolves at parse-time.
/// </summary>
public class ConstDbEntityConfiguration : IEntityTypeConfiguration<ConstDbEntity>
{
    public void Configure(EntityTypeBuilder<ConstDbEntity> b)
    {
        b.ToTable("const_db");
        b.HasKey(e => e.Name);
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        b.Property(e => e.Value).HasColumnName("value");
        b.Property(e => e.IsParameter).HasColumnName("is_parameter");
    }
}

// ============================================================================
// DB-8a: tier-1 re-normalized catalogs (replaces PayloadJson blobs)
// ============================================================================

/// <summary>
/// DB-8a: level-gap penalty parent row. Replaces the prior
/// LevelPenaltyEntity (PayloadStringKeyEntity) JSON blob with a typed
/// parent + <c>level_penalty_difference_db</c> child table.
/// </summary>
public class LevelPenaltyDbEntityConfiguration : IEntityTypeConfiguration<LevelPenaltyDbEntity>
{
    public void Configure(EntityTypeBuilder<LevelPenaltyDbEntity> b)
    {
        b.ToTable("level_penalty_db");
        b.HasKey(e => e.PenaltyType);
        b.Property(e => e.PenaltyType).HasColumnName("penalty_type").HasMaxLength(32).IsRequired();
    }
}

/// <summary>
/// DB-8a: per-level-difference rate row in the
/// <see cref="LevelPenaltyDbEntity"/> curve. Composite key
/// (penalty_type, difference).
/// </summary>
public class LevelPenaltyDifferenceDbEntityConfiguration : IEntityTypeConfiguration<LevelPenaltyDifferenceDbEntity>
{
    public void Configure(EntityTypeBuilder<LevelPenaltyDifferenceDbEntity> b)
    {
        b.ToTable("level_penalty_difference_db");
        b.HasKey(e => new { e.PenaltyType, e.Difference });
        b.Property(e => e.PenaltyType).HasColumnName("penalty_type").HasMaxLength(32).IsRequired();
        b.Property(e => e.Difference).HasColumnName("difference");
        b.Property(e => e.Rate).HasColumnName("rate");
    }
}

/// <summary>
/// DB-8a: element vs element damage matrix. Replaces AttrFixEntity
/// (PayloadIntKeyEntity) blob. Composite key (level, attacker_element,
/// defender_element); each row is a single percentage multiplier.
/// </summary>
public class AttrFixDbEntityConfiguration : IEntityTypeConfiguration<AttrFixDbEntity>
{
    public void Configure(EntityTypeBuilder<AttrFixDbEntity> b)
    {
        b.ToTable("attr_fix_db");
        b.HasKey(e => new { e.Level, e.AttackerElement, e.DefenderElement });
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.AttackerElement).HasColumnName("attacker_element").HasMaxLength(24).IsRequired();
        b.Property(e => e.DefenderElement).HasColumnName("defender_element").HasMaxLength(24).IsRequired();
        b.Property(e => e.Multiplier).HasColumnName("multiplier");
    }
}

/// <summary>
/// DB-8a: reputation faction bundle parent row. Replaces
/// ReputationGroupEntity (PayloadIntKeyEntity). Members hang off
/// <see cref="ReputationGroupMemberDbEntity"/>.
/// </summary>
public class ReputationGroupDbEntityConfiguration : IEntityTypeConfiguration<ReputationGroupDbEntity>
{
    public void Configure(EntityTypeBuilder<ReputationGroupDbEntity> b)
    {
        b.ToTable("reputation_group_db");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.ScriptName).HasColumnName("script_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
    }
}

/// <summary>
/// DB-8a: child rows of <see cref="ReputationGroupDbEntity"/>.
/// Composite key (group_id, reputation_id).
/// </summary>
public class ReputationGroupMemberDbEntityConfiguration : IEntityTypeConfiguration<ReputationGroupMemberDbEntity>
{
    public void Configure(EntityTypeBuilder<ReputationGroupMemberDbEntity> b)
    {
        b.ToTable("reputation_group_member_db");
        b.HasKey(e => new { e.GroupId, e.ReputationId });
        b.Property(e => e.GroupId).HasColumnName("group_id");
        b.Property(e => e.ReputationId).HasColumnName("reputation_id");
    }
}

// ============================================================================
// DB-8b: tier-2 single-child re-normalized catalogs
// ============================================================================

public class MobSummonDbEntityConfiguration : IEntityTypeConfiguration<MobSummonDbEntity>
{
    public void Configure(EntityTypeBuilder<MobSummonDbEntity> b)
    {
        b.ToTable("mob_summon_db");
        b.HasKey(e => e.GroupName);
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.DefaultMobAegis).HasColumnName("default_mob_aegis").HasMaxLength(64).IsRequired();
    }
}

public class MobSummonEntryDbEntityConfiguration : IEntityTypeConfiguration<MobSummonEntryDbEntity>
{
    public void Configure(EntityTypeBuilder<MobSummonEntryDbEntity> b)
    {
        b.ToTable("mob_summon_entry_db");
        b.HasKey(e => new { e.GroupName, e.MobAegis });
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.MobAegis).HasColumnName("mob_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Rate).HasColumnName("rate");
    }
}

public class AttendanceCatalogDbEntityConfiguration : IEntityTypeConfiguration<AttendanceCatalogDbEntity>
{
    public void Configure(EntityTypeBuilder<AttendanceCatalogDbEntity> b)
    {
        b.ToTable("attendance_catalog_db");
        b.HasKey(e => e.AttendanceId);
        b.Property(e => e.AttendanceId).HasColumnName("attendance_id");
        b.Property(e => e.StartDate).HasColumnName("start_date");
        b.Property(e => e.EndDate).HasColumnName("end_date");
    }
}

public class AttendanceCatalogRewardDbEntityConfiguration : IEntityTypeConfiguration<AttendanceCatalogRewardDbEntity>
{
    public void Configure(EntityTypeBuilder<AttendanceCatalogRewardDbEntity> b)
    {
        b.ToTable("attendance_catalog_reward_db");
        b.HasKey(e => new { e.AttendanceId, e.Day });
        b.Property(e => e.AttendanceId).HasColumnName("attendance_id");
        b.Property(e => e.Day).HasColumnName("day");
        b.Property(e => e.ItemId).HasColumnName("item_id");
        b.Property(e => e.Amount).HasColumnName("amount");
    }
}

public class ItemCashDbEntityConfiguration : IEntityTypeConfiguration<ItemCashDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemCashDbEntity> b)
    {
        b.ToTable("item_cash_db");
        b.HasKey(e => e.Tab);
        b.Property(e => e.Tab).HasColumnName("tab").HasMaxLength(32).IsRequired();
    }
}

public class ItemCashEntryDbEntityConfiguration : IEntityTypeConfiguration<ItemCashEntryDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemCashEntryDbEntity> b)
    {
        b.ToTable("item_cash_entry_db");
        b.HasKey(e => new { e.Tab, e.ItemAegis });
        b.Property(e => e.Tab).HasColumnName("tab").HasMaxLength(32).IsRequired();
        b.Property(e => e.ItemAegis).HasColumnName("item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Price).HasColumnName("price");
    }
}

public class ItemGroupCatalogDbEntityConfiguration : IEntityTypeConfiguration<ItemGroupCatalogDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemGroupCatalogDbEntity> b)
    {
        b.ToTable("item_group_catalog_db");
        b.HasKey(e => e.GroupName);
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64).IsRequired();
    }
}

public class ItemGroupCatalogEntryDbEntityConfiguration : IEntityTypeConfiguration<ItemGroupCatalogEntryDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemGroupCatalogEntryDbEntity> b)
    {
        b.ToTable("item_group_catalog_entry_db");
        b.HasKey(e => new { e.GroupName, e.SubGroup, e.Index });
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.SubGroup).HasColumnName("sub_group");
        b.Property(e => e.Index).HasColumnName("entry_index");
        b.Property(e => e.ItemAegis).HasColumnName("item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Rate).HasColumnName("rate");
        b.Property(e => e.Announced).HasColumnName("announced");
        b.Property(e => e.Amount).HasColumnName("amount");
        b.Property(e => e.DurationHours).HasColumnName("duration_hours");
        b.Property(e => e.Refine).HasColumnName("refine");
        b.Property(e => e.RandomOptionGroup).HasColumnName("random_option_group").HasMaxLength(64);
    }
}

public class ItemPackageDbEntityConfiguration : IEntityTypeConfiguration<ItemPackageDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemPackageDbEntity> b)
    {
        b.ToTable("item_package_db");
        b.HasKey(e => e.ItemAegis);
        b.Property(e => e.ItemAegis).HasColumnName("item_aegis").HasMaxLength(64).IsRequired();
    }
}

public class ItemPackageEntryDbEntityConfiguration : IEntityTypeConfiguration<ItemPackageEntryDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemPackageEntryDbEntity> b)
    {
        b.ToTable("item_package_entry_db");
        b.HasKey(e => new { e.ItemAegis, e.GroupId, e.ContainedItemAegis });
        b.Property(e => e.ItemAegis).HasColumnName("item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.GroupId).HasColumnName("group_id");
        b.Property(e => e.ContainedItemAegis).HasColumnName("contained_item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Amount).HasColumnName("amount");
        b.Property(e => e.Refine).HasColumnName("refine");
        b.Property(e => e.RentalHours).HasColumnName("rental_hours");
        b.Property(e => e.RandomOptionGroup).HasColumnName("random_option_group").HasMaxLength(64);
    }
}

public class ItemComboDbEntityConfiguration : IEntityTypeConfiguration<ItemComboDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemComboDbEntity> b)
    {
        b.ToTable("item_combo_db");
        b.HasKey(e => e.ComboId);
        b.Property(e => e.ComboId).HasColumnName("combo_id");
        b.Property(e => e.Script).HasColumnName("script").HasColumnType("text").IsRequired();
    }
}

public class ItemComboMemberDbEntityConfiguration : IEntityTypeConfiguration<ItemComboMemberDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemComboMemberDbEntity> b)
    {
        b.ToTable("item_combo_member_db");
        b.HasKey(e => new { e.ComboId, e.MemberItemAegis });
        b.Property(e => e.ComboId).HasColumnName("combo_id");
        b.Property(e => e.MemberItemAegis).HasColumnName("member_item_aegis").HasMaxLength(64).IsRequired();
    }
}

// ============================================================================
// DB-8c: skill tree re-normalized catalogs
// ============================================================================

public class SkillTreeDbEntityConfiguration : IEntityTypeConfiguration<SkillTreeDbEntity>
{
    public void Configure(EntityTypeBuilder<SkillTreeDbEntity> b)
    {
        b.ToTable("skill_tree_db");
        b.HasKey(e => e.JobAegis);
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
    }
}

public class SkillTreeInheritDbEntityConfiguration : IEntityTypeConfiguration<SkillTreeInheritDbEntity>
{
    public void Configure(EntityTypeBuilder<SkillTreeInheritDbEntity> b)
    {
        b.ToTable("skill_tree_inherit_db");
        b.HasKey(e => new { e.ChildJobAegis, e.ParentJobAegis });
        b.Property(e => e.ChildJobAegis).HasColumnName("child_job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.ParentJobAegis).HasColumnName("parent_job_aegis").HasMaxLength(64).IsRequired();
    }
}

public class SkillTreeEntryDbEntityConfiguration : IEntityTypeConfiguration<SkillTreeEntryDbEntity>
{
    public void Configure(EntityTypeBuilder<SkillTreeEntryDbEntity> b)
    {
        b.ToTable("skill_tree_entry_db");
        b.HasKey(e => new { e.JobAegis, e.SkillAegis });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxLevel).HasColumnName("max_level");
        b.Property(e => e.Exclude).HasColumnName("exclude");
    }
}

public class SkillTreeRequirementDbEntityConfiguration : IEntityTypeConfiguration<SkillTreeRequirementDbEntity>
{
    public void Configure(EntityTypeBuilder<SkillTreeRequirementDbEntity> b)
    {
        b.ToTable("skill_tree_requirement_db");
        b.HasKey(e => new { e.JobAegis, e.SkillAegis, e.RequiredSkillAegis });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.RequiredSkillAegis).HasColumnName("required_skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.RequiredLevel).HasColumnName("required_level");
    }
}

public class GuildSkillTreeDbEntityConfiguration : IEntityTypeConfiguration<GuildSkillTreeDbEntity>
{
    public void Configure(EntityTypeBuilder<GuildSkillTreeDbEntity> b)
    {
        b.ToTable("guild_skill_tree_db");
        b.HasKey(e => e.SkillAegis);
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxLevel).HasColumnName("max_level");
    }
}

public class GuildSkillTreeRequirementDbEntityConfiguration : IEntityTypeConfiguration<GuildSkillTreeRequirementDbEntity>
{
    public void Configure(EntityTypeBuilder<GuildSkillTreeRequirementDbEntity> b)
    {
        b.ToTable("guild_skill_tree_requirement_db");
        b.HasKey(e => new { e.SkillAegis, e.RequiredSkillAegis });
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.RequiredSkillAegis).HasColumnName("required_skill_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.RequiredLevel).HasColumnName("required_level");
    }
}

// ============================================================================
// DB-8d: job table re-normalized catalogs
// ============================================================================

public class JobInfoDbEntityConfiguration : IEntityTypeConfiguration<JobInfoDbEntity>
{
    public void Configure(EntityTypeBuilder<JobInfoDbEntity> b)
    {
        b.ToTable("job_info_db");
        b.HasKey(e => e.JobAegis);
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxWeight).HasColumnName("max_weight");
        b.Property(e => e.HpFactor).HasColumnName("hp_factor");
        b.Property(e => e.HpIncrease).HasColumnName("hp_increase");
        b.Property(e => e.SpFactor).HasColumnName("sp_factor");
        b.Property(e => e.SpIncrease).HasColumnName("sp_increase");
    }
}

public class JobBonusStatsDbEntityConfiguration : IEntityTypeConfiguration<JobBonusStatsDbEntity>
{
    public void Configure(EntityTypeBuilder<JobBonusStatsDbEntity> b)
    {
        b.ToTable("job_bonus_stats_db");
        b.HasKey(e => new { e.JobAegis, e.Level });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Str).HasColumnName("str");
        b.Property(e => e.Agi).HasColumnName("agi");
        b.Property(e => e.Vit).HasColumnName("vit");
        b.Property(e => e.IntStat).HasColumnName("int_stat");
        b.Property(e => e.Dex).HasColumnName("dex");
        b.Property(e => e.Luk).HasColumnName("luk");
        b.Property(e => e.Pow).HasColumnName("pow");
        b.Property(e => e.Sta).HasColumnName("sta");
        b.Property(e => e.Wis).HasColumnName("wis");
        b.Property(e => e.Spl).HasColumnName("spl");
        b.Property(e => e.Con).HasColumnName("con");
        b.Property(e => e.Crt).HasColumnName("crt");
    }
}

public class JobExpDbEntityConfiguration : IEntityTypeConfiguration<JobExpDbEntity>
{
    public void Configure(EntityTypeBuilder<JobExpDbEntity> b)
    {
        b.ToTable("job_exp_db");
        b.HasKey(e => new { e.JobAegis, e.Level });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.BaseExp).HasColumnName("base_exp");
        b.Property(e => e.JobExp).HasColumnName("job_exp");
    }
}

public class JobMaxLevelDbEntityConfiguration : IEntityTypeConfiguration<JobMaxLevelDbEntity>
{
    public void Configure(EntityTypeBuilder<JobMaxLevelDbEntity> b)
    {
        b.ToTable("job_max_level_db");
        b.HasKey(e => e.JobAegis);
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaxBaseLevel).HasColumnName("max_base_level");
        b.Property(e => e.MaxJobLevel).HasColumnName("max_job_level");
    }
}

public class JobBasePointsDbEntityConfiguration : IEntityTypeConfiguration<JobBasePointsDbEntity>
{
    public void Configure(EntityTypeBuilder<JobBasePointsDbEntity> b)
    {
        b.ToTable("job_base_points_db");
        b.HasKey(e => new { e.JobAegis, e.Level });
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Hp).HasColumnName("hp");
        b.Property(e => e.Sp).HasColumnName("sp");
        b.Property(e => e.Ap).HasColumnName("ap");
    }
}

// ============================================================================
// DB-8e: status.yml re-normalized
// ============================================================================

public class StatusDbEntityConfiguration : IEntityTypeConfiguration<StatusDbEntity>
{
    public void Configure(EntityTypeBuilder<StatusDbEntity> b)
    {
        b.ToTable("status_db");
        b.HasKey(e => e.StatusName);
        b.Property(e => e.StatusName).HasColumnName("status_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.DurationLookup).HasColumnName("duration_lookup").HasMaxLength(64);
        b.Property(e => e.Opt1).HasColumnName("opt1").HasMaxLength(32);
        b.Property(e => e.Opt2).HasColumnName("opt2").HasMaxLength(32);
        b.Property(e => e.Opt3).HasColumnName("opt3").HasMaxLength(32);
    }
}

public class StatusDbFlagEntityConfiguration : IEntityTypeConfiguration<StatusDbFlagEntity>
{
    public void Configure(EntityTypeBuilder<StatusDbFlagEntity> b)
    {
        b.ToTable("status_db_flag");
        b.HasKey(e => new { e.StatusName, e.Category, e.FlagName });
        b.Property(e => e.StatusName).HasColumnName("status_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.Category).HasColumnName("category").HasMaxLength(16).IsRequired();
        b.Property(e => e.FlagName).HasColumnName("flag_name").HasMaxLength(64).IsRequired();
    }
}

// ============================================================================
// DB-8f: battleground + elemental re-normalized
// ============================================================================

public class BattlegroundTypeDbEntityConfiguration : IEntityTypeConfiguration<BattlegroundTypeDbEntity>
{
    public void Configure(EntityTypeBuilder<BattlegroundTypeDbEntity> b)
    {
        b.ToTable("battleground_type_db");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        b.Property(e => e.MinPlayers).HasColumnName("min_players");
        b.Property(e => e.MinLevel).HasColumnName("min_level");
    }
}

public class BattlegroundJobRestrictionDbEntityConfiguration : IEntityTypeConfiguration<BattlegroundJobRestrictionDbEntity>
{
    public void Configure(EntityTypeBuilder<BattlegroundJobRestrictionDbEntity> b)
    {
        b.ToTable("battleground_job_restriction_db");
        b.HasKey(e => new { e.BgId, e.JobAegis });
        b.Property(e => e.BgId).HasColumnName("bg_id");
        b.Property(e => e.JobAegis).HasColumnName("job_aegis").HasMaxLength(64).IsRequired();
    }
}

public class BattlegroundLocationDbEntityConfiguration : IEntityTypeConfiguration<BattlegroundLocationDbEntity>
{
    public void Configure(EntityTypeBuilder<BattlegroundLocationDbEntity> b)
    {
        b.ToTable("battleground_location_db");
        b.HasKey(e => new { e.BgId, e.MapName });
        b.Property(e => e.BgId).HasColumnName("bg_id");
        b.Property(e => e.MapName).HasColumnName("map_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.StartEvent).HasColumnName("start_event").HasMaxLength(128);
        b.Property(e => e.TeamARespawnX).HasColumnName("team_a_respawn_x");
        b.Property(e => e.TeamARespawnY).HasColumnName("team_a_respawn_y");
        b.Property(e => e.TeamAQuitEvent).HasColumnName("team_a_quit_event").HasMaxLength(128);
        b.Property(e => e.TeamAActiveEvent).HasColumnName("team_a_active_event").HasMaxLength(128);
        b.Property(e => e.TeamAVariable).HasColumnName("team_a_variable").HasMaxLength(64);
        b.Property(e => e.TeamBRespawnX).HasColumnName("team_b_respawn_x");
        b.Property(e => e.TeamBRespawnY).HasColumnName("team_b_respawn_y");
        b.Property(e => e.TeamBQuitEvent).HasColumnName("team_b_quit_event").HasMaxLength(128);
        b.Property(e => e.TeamBActiveEvent).HasColumnName("team_b_active_event").HasMaxLength(128);
        b.Property(e => e.TeamBVariable).HasColumnName("team_b_variable").HasMaxLength(64);
    }
}

public class ElementalCatalogDbEntityConfiguration : IEntityTypeConfiguration<ElementalCatalogDbEntity>
{
    public void Configure(EntityTypeBuilder<ElementalCatalogDbEntity> b)
    {
        b.ToTable("elemental_catalog_db");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.AegisName).HasColumnName("aegis_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        b.Property(e => e.Level).HasColumnName("level");
        b.Property(e => e.Size).HasColumnName("size").HasMaxLength(24);
        b.Property(e => e.Element).HasColumnName("element").HasMaxLength(24);
        b.Property(e => e.ElementLevel).HasColumnName("element_level");
    }
}

public class ElementalModeDbEntityConfiguration : IEntityTypeConfiguration<ElementalModeDbEntity>
{
    public void Configure(EntityTypeBuilder<ElementalModeDbEntity> b)
    {
        b.ToTable("elemental_mode_db");
        b.HasKey(e => new { e.ElementalId, e.Mode });
        b.Property(e => e.ElementalId).HasColumnName("elemental_id");
        b.Property(e => e.Mode).HasColumnName("mode").HasMaxLength(16).IsRequired();
        b.Property(e => e.SkillAegis).HasColumnName("skill_aegis").HasMaxLength(64).IsRequired();
    }
}

// ============================================================================
// DB-8g: enchant pipeline configurations
// ============================================================================

public class ItemEnchantDbEntityConfiguration : IEntityTypeConfiguration<ItemEnchantDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemEnchantDbEntity> b)
    {
        b.ToTable("item_enchant_db");
        b.HasKey(e => e.EnchantId);
        b.Property(e => e.EnchantId).HasColumnName("enchant_id");
        b.Property(e => e.MinimumRefine).HasColumnName("minimum_refine");
        b.Property(e => e.ResetChance).HasColumnName("reset_chance");
        b.Property(e => e.ResetPrice).HasColumnName("reset_price");
    }
}

public class ItemEnchantTargetDbEntityConfiguration : IEntityTypeConfiguration<ItemEnchantTargetDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemEnchantTargetDbEntity> b)
    {
        b.ToTable("item_enchant_target_db");
        b.HasKey(e => new { e.EnchantId, e.ItemAegis });
        b.Property(e => e.EnchantId).HasColumnName("enchant_id");
        b.Property(e => e.ItemAegis).HasColumnName("item_aegis").HasMaxLength(64).IsRequired();
    }
}

public class ItemEnchantMaterialDbEntityConfiguration : IEntityTypeConfiguration<ItemEnchantMaterialDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemEnchantMaterialDbEntity> b)
    {
        b.ToTable("item_enchant_material_db");
        b.HasKey(e => new { e.EnchantId, e.Slot, e.MaterialAegis });
        b.Property(e => e.EnchantId).HasColumnName("enchant_id");
        b.Property(e => e.Slot).HasColumnName("slot");
        b.Property(e => e.MaterialAegis).HasColumnName("material_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Amount).HasColumnName("amount");
    }
}

public class ItemEnchantSlotDbEntityConfiguration : IEntityTypeConfiguration<ItemEnchantSlotDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemEnchantSlotDbEntity> b)
    {
        b.ToTable("item_enchant_slot_db");
        b.HasKey(e => new { e.EnchantId, e.Slot });
        b.Property(e => e.EnchantId).HasColumnName("enchant_id");
        b.Property(e => e.Slot).HasColumnName("slot");
        b.Property(e => e.Price).HasColumnName("price");
        b.Property(e => e.OrderIndex).HasColumnName("order_index");
    }
}

public class ItemEnchantOptionDbEntityConfiguration : IEntityTypeConfiguration<ItemEnchantOptionDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemEnchantOptionDbEntity> b)
    {
        b.ToTable("item_enchant_option_db");
        b.HasKey(e => new { e.EnchantId, e.Slot, e.EnchantGrade, e.OptionItemAegis });
        b.Property(e => e.EnchantId).HasColumnName("enchant_id");
        b.Property(e => e.Slot).HasColumnName("slot");
        b.Property(e => e.EnchantGrade).HasColumnName("enchant_grade");
        b.Property(e => e.OptionItemAegis).HasColumnName("option_item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.Chance).HasColumnName("chance");
    }
}

public class ItemReformDbEntityConfiguration : IEntityTypeConfiguration<ItemReformDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemReformDbEntity> b)
    {
        b.ToTable("item_reform_db");
        b.HasKey(e => e.ResultItemAegis);
        b.Property(e => e.ResultItemAegis).HasColumnName("result_item_aegis").HasMaxLength(64).IsRequired();
    }
}

public class ItemReformBaseDbEntityConfiguration : IEntityTypeConfiguration<ItemReformBaseDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemReformBaseDbEntity> b)
    {
        b.ToTable("item_reform_base_db");
        b.HasKey(e => new { e.ResultItemAegis, e.BaseItemAegis });
        b.Property(e => e.ResultItemAegis).HasColumnName("result_item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.BaseItemAegis).HasColumnName("base_item_aegis").HasMaxLength(64).IsRequired();
        b.Property(e => e.MaximumRefine).HasColumnName("maximum_refine");
        b.Property(e => e.ChangeRefine).HasColumnName("change_refine");
        b.Property(e => e.ResultItemOverride).HasColumnName("result_item_override").HasMaxLength(64);
        b.Property(e => e.RandomOptionGroup).HasColumnName("random_option_group").HasMaxLength(64);
        b.Property(e => e.ClearSlots).HasColumnName("clear_slots");
        b.Property(e => e.RemoveEnchantgrade).HasColumnName("remove_enchantgrade");
        b.Property(e => e.CardsAllowed).HasColumnName("cards_allowed");
    }
}

public class LaphineSynthesisDbEntityConfiguration : IEntityTypeConfiguration<LaphineSynthesisDbEntity>
{
    public void Configure(EntityTypeBuilder<LaphineSynthesisDbEntity> b)
    {
        b.ToTable("laphine_synthesis_db");
        b.HasKey(e => e.RecipeItem);
        b.Property(e => e.RecipeItem).HasColumnName("recipe_item").HasMaxLength(64).IsRequired();
        b.Property(e => e.RewardGroup).HasColumnName("reward_group").HasMaxLength(64);
        b.Property(e => e.RequiredRequirementsCount).HasColumnName("required_requirements_count");
    }
}

public class LaphineSynthesisRequirementDbEntityConfiguration : IEntityTypeConfiguration<LaphineSynthesisRequirementDbEntity>
{
    public void Configure(EntityTypeBuilder<LaphineSynthesisRequirementDbEntity> b)
    {
        b.ToTable("laphine_synthesis_requirement_db");
        b.HasKey(e => new { e.RecipeItem, e.RequirementItem });
        b.Property(e => e.RecipeItem).HasColumnName("recipe_item").HasMaxLength(64).IsRequired();
        b.Property(e => e.RequirementItem).HasColumnName("requirement_item").HasMaxLength(64).IsRequired();
        b.Property(e => e.RefineMin).HasColumnName("refine_min");
        b.Property(e => e.RefineMax).HasColumnName("refine_max");
    }
}

public class LaphineUpgradeDbEntityConfiguration : IEntityTypeConfiguration<LaphineUpgradeDbEntity>
{
    public void Configure(EntityTypeBuilder<LaphineUpgradeDbEntity> b)
    {
        b.ToTable("laphine_upgrade_db");
        b.HasKey(e => e.UpgradeItem);
        b.Property(e => e.UpgradeItem).HasColumnName("upgrade_item").HasMaxLength(64).IsRequired();
        b.Property(e => e.RandomOptionGroup).HasColumnName("random_option_group");
        b.Property(e => e.MinimumRefine).HasColumnName("minimum_refine");
    }
}

public class LaphineUpgradeTargetDbEntityConfiguration : IEntityTypeConfiguration<LaphineUpgradeTargetDbEntity>
{
    public void Configure(EntityTypeBuilder<LaphineUpgradeTargetDbEntity> b)
    {
        b.ToTable("laphine_upgrade_target_db");
        b.HasKey(e => new { e.UpgradeItem, e.TargetItem });
        b.Property(e => e.UpgradeItem).HasColumnName("upgrade_item").HasMaxLength(64).IsRequired();
        b.Property(e => e.TargetItem).HasColumnName("target_item").HasMaxLength(64).IsRequired();
    }
}

public class ItemRandomOptGroupDbEntityConfiguration : IEntityTypeConfiguration<ItemRandomOptGroupDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemRandomOptGroupDbEntity> b)
    {
        b.ToTable("item_randomopt_group_db");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(64).IsRequired();
    }
}

public class ItemRandomOptGroupOptionDbEntityConfiguration : IEntityTypeConfiguration<ItemRandomOptGroupOptionDbEntity>
{
    public void Configure(EntityTypeBuilder<ItemRandomOptGroupOptionDbEntity> b)
    {
        b.ToTable("item_randomopt_group_option_db");
        b.HasKey(e => new { e.GroupId, e.Slot, e.OptionName });
        b.Property(e => e.GroupId).HasColumnName("group_id");
        b.Property(e => e.Slot).HasColumnName("slot");
        b.Property(e => e.OptionName).HasColumnName("option_name").HasMaxLength(64).IsRequired();
        b.Property(e => e.MinValue).HasColumnName("min_value");
        b.Property(e => e.MaxValue).HasColumnName("max_value");
        b.Property(e => e.Chance).HasColumnName("chance");
    }
}
