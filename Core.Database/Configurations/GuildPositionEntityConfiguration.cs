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
    }
}
