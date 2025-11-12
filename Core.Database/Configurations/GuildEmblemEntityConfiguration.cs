using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildEmblemEntityConfiguration : IEntityTypeConfiguration<GuildEmblemEntity>
{
    public void Configure(EntityTypeBuilder<GuildEmblemEntity> builder)
    {
        builder.ToTable("guild_emblems");
        builder.HasKey(e => new { e.WorldName, e.GuildId });
        
        builder.Property(e => e.WorldName).HasColumnName("world_name").HasMaxLength(32).IsRequired();
        builder.Property(e => e.GuildId).HasColumnName("guild_id");
        builder.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(255).IsRequired();
        builder.Property(e => e.FileData).HasColumnName("file_data");
        builder.Property(e => e.Version).HasColumnName("version").HasDefaultValue(0u);
    }
}
