using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildSkillEntityConfiguration : IEntityTypeConfiguration<GuildSkillEntity>
{
    public void Configure(EntityTypeBuilder<GuildSkillEntity> builder)
    {
        builder.ToTable("guild_skill");
        builder.HasKey(e => new { e.GuildId, e.Id });
    }
}
