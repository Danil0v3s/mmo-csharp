using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MobSpawnEntityConfiguration : IEntityTypeConfiguration<MobSpawnEntity>
{
    public void Configure(EntityTypeBuilder<MobSpawnEntity> builder)
    {
        builder.ToTable("mob_spawn");
        builder.HasKey(e => e.SpawnId);

        builder.Property(e => e.SpawnId).HasColumnName("spawn_id").ValueGeneratedOnAdd();
        builder.Property(e => e.MapName).HasColumnName("map_name").HasMaxLength(24).IsRequired();
        builder.Property(e => e.CenterX).HasColumnName("center_x");
        builder.Property(e => e.CenterY).HasColumnName("center_y");
        builder.Property(e => e.SpanXs).HasColumnName("span_xs").HasDefaultValue((short)0);
        builder.Property(e => e.SpanYs).HasColumnName("span_ys").HasDefaultValue((short)0);

        builder.Property(e => e.IsBoss).HasColumnName("is_boss").HasDefaultValue(false);
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(64).IsRequired().HasDefaultValue("");

        builder.Property(e => e.MobId).HasColumnName("mob_id");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Delay1).HasColumnName("delay1").HasDefaultValue(0);
        builder.Property(e => e.Delay2).HasColumnName("delay2").HasDefaultValue(0);

        builder.Property(e => e.EventLabel).HasColumnName("event_label").HasMaxLength(64).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Size).HasColumnName("size").HasDefaultValue(0);
        builder.Property(e => e.Ai).HasColumnName("ai").HasDefaultValue(0);

        // The spawn manager iterates these per loaded map at startup.
        builder.HasIndex(e => e.MapName).HasDatabaseName("ix_mob_spawn_map");
    }
}
