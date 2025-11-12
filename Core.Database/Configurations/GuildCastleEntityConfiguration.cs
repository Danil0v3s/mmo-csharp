using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildCastleEntityConfiguration : IEntityTypeConfiguration<GuildCastleEntity>
{
    public void Configure(EntityTypeBuilder<GuildCastleEntity> builder)
    {
        builder.ToTable("guild_castle");
        builder.HasKey(e => e.CastleId);
    }
}
