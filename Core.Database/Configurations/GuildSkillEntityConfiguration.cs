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
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValue((ushort)0);
        builder.Property(e => e.Lv).HasColumnName("lv").HasDefaultValue((byte)0);
    }
}
