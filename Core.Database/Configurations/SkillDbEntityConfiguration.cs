using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillDbEntityConfiguration : IEntityTypeConfiguration<SkillDbEntity>
{
    public void Configure(EntityTypeBuilder<SkillDbEntity> builder)
    {
        builder.ToTable("skill_db");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(e => e.MaxLevel).HasColumnName("max_level");
        builder.Property(e => e.TargetMode).HasColumnName("target_mode").HasMaxLength(20);
        builder.Property(e => e.DamageKind).HasColumnName("damage_kind").HasMaxLength(20);
        builder.Property(e => e.Range).HasColumnName("range");
        builder.Property(e => e.Element).HasColumnName("element").HasMaxLength(20);
        builder.Property(e => e.StatusType).HasColumnName("status_type").HasMaxLength(40);

        // Per-level packed CSV columns — rAthena's skill_db.yml flattens
        // these into per-level keys; we keep them as colon-delimited
        // strings to mirror the rAthena `skill_db.txt` packed format that
        // some installs still use.
        builder.Property(e => e.SpCost).HasColumnName("sp_cost").HasMaxLength(255);
        builder.Property(e => e.CastTimeMs).HasColumnName("cast_time_ms").HasMaxLength(255);
        builder.Property(e => e.CooldownMs).HasColumnName("cooldown_ms").HasMaxLength(255);
        builder.Property(e => e.DamageRate).HasColumnName("damage_rate").HasMaxLength(255);
        builder.Property(e => e.EffectAmount).HasColumnName("effect_amount").HasMaxLength(255);
        builder.Property(e => e.StatusDurationMs).HasColumnName("status_duration_ms").HasMaxLength(255);

        // COMBAT-92 — Requirements / Flags / Unit columns (pipe-delimited name tokens + ammo qty).
        builder.Property(e => e.Ammo).HasColumnName("ammo").HasMaxLength(80);
        builder.Property(e => e.AmmoAmount).HasColumnName("ammo_amount");
        builder.Property(e => e.Inf2).HasColumnName("inf2").HasMaxLength(255);
        builder.Property(e => e.UnitFlags).HasColumnName("unit_flags").HasMaxLength(255);
    }
}
