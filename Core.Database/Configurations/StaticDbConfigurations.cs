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
