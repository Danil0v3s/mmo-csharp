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
    }
}
