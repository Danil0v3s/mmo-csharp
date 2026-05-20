using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MobSkillDbEntityConfiguration : IEntityTypeConfiguration<MobSkillDbEntity>
{
    public void Configure(EntityTypeBuilder<MobSkillDbEntity> builder)
    {
        builder.ToTable("mob_skill_db");

        // rAthena ships this table without a primary key (MyISAM); we
        // compose one from (MobId, Info) — Info is the unique
        // "MobName@SKILL_NAME" key per mob → no collisions.
        builder.HasKey(e => new { e.MobId, e.Info });

        builder.Property(e => e.MobId).HasColumnName("MOB_ID");
        builder.Property(e => e.Info).HasColumnName("INFO").HasMaxLength(255).IsRequired();
        builder.Property(e => e.State).HasColumnName("STATE").HasColumnType("text").IsRequired();
        builder.Property(e => e.SkillId).HasColumnName("SKILL_ID");
        builder.Property(e => e.SkillLv).HasColumnName("SKILL_LV");
        builder.Property(e => e.Rate).HasColumnName("RATE");
        builder.Property(e => e.CastTime).HasColumnName("CASTTIME");
        builder.Property(e => e.Delay).HasColumnName("DELAY");
        builder.Property(e => e.Cancelable).HasColumnName("CANCELABLE").HasColumnType("text").IsRequired();
        builder.Property(e => e.Target).HasColumnName("TARGET").HasColumnType("text").IsRequired();
        builder.Property(e => e.Condition).HasColumnName("CONDITION").HasColumnType("text").IsRequired();
        builder.Property(e => e.ConditionValue).HasColumnName("CONDITION_VALUE").HasColumnType("text");
        builder.Property(e => e.Val1).HasColumnName("VAL1");
        builder.Property(e => e.Val2).HasColumnName("VAL2");
        builder.Property(e => e.Val3).HasColumnName("VAL3");
        builder.Property(e => e.Val4).HasColumnName("VAL4");
        builder.Property(e => e.Val5).HasColumnName("VAL5");
        builder.Property(e => e.Emotion).HasColumnName("EMOTION").HasColumnType("text");
        builder.Property(e => e.Chat).HasColumnName("CHAT").HasColumnType("text");

        builder.HasIndex(e => e.MobId);
    }
}
