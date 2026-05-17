using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MapFlagEntityConfiguration : IEntityTypeConfiguration<MapFlagEntity>
{
    public void Configure(EntityTypeBuilder<MapFlagEntity> builder)
    {
        builder.ToTable("map_flag");
        builder.HasKey(e => e.FlagId);

        builder.Property(e => e.FlagId).HasColumnName("flag_id").ValueGeneratedOnAdd();
        builder.Property(e => e.MapName).HasColumnName("map_name").HasMaxLength(24).IsRequired();
        builder.Property(e => e.Flag).HasColumnName("flag").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(128).IsRequired().HasDefaultValue("");

        // Map server queries by map_name to assemble the flag set for
        // each loaded map. (map_name, flag) is the unique combination
        // in rAthena's input but the source files have duplicates we
        // pass through verbatim — index, don't unique-constrain.
        builder.HasIndex(e => e.MapName).HasDatabaseName("ix_map_flag_map");
    }
}
