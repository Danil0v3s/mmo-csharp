using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildExpulsionEntityConfiguration : IEntityTypeConfiguration<GuildExpulsionEntity>
{
    public void Configure(EntityTypeBuilder<GuildExpulsionEntity> builder)
    {
        builder.ToTable("guild_expulsion");
        builder.HasKey(e => new { e.GuildId, e.Name });
    }
}
