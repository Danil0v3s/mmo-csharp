using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildAllianceEntityConfiguration : IEntityTypeConfiguration<GuildAllianceEntity>
{
    public void Configure(EntityTypeBuilder<GuildAllianceEntity> builder)
    {
        builder.ToTable("guild_alliance");
        builder.HasKey(e => new { e.GuildId, e.AllianceId });
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.Opposition).HasColumnName("opposition").HasDefaultValue(0u);
        builder.Property(e => e.AllianceId).HasColumnName("alliance_id").HasDefaultValue(0u);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.AllianceId).HasDatabaseName("alliance_id");
    }
}
