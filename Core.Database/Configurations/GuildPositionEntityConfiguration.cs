using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildPositionEntityConfiguration : IEntityTypeConfiguration<GuildPositionEntity>
{
    public void Configure(EntityTypeBuilder<GuildPositionEntity> builder)
    {
        builder.ToTable("guild_position");
        builder.HasKey(e => new { e.GuildId, e.Position });
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.Position).HasColumnName("position").HasDefaultValue((byte)0);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Mode).HasColumnName("mode").HasDefaultValue((ushort)0);
        builder.Property(e => e.ExpMode).HasColumnName("exp_mode").HasDefaultValue((byte)0);
    }
}
