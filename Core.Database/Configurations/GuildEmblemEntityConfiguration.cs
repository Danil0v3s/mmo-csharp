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
    }
}
