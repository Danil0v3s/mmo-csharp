using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillCooldownMercenaryEntityConfiguration : IEntityTypeConfiguration<SkillCooldownMercenaryEntity>
{
    public void Configure(EntityTypeBuilder<SkillCooldownMercenaryEntity> builder)
    {
        builder.ToTable("skillcooldown_mercenary");
        builder.HasKey(e => new { e.MerId, e.Skill });
        
        builder.Property(e => e.MerId).HasColumnName("mer_id");
        builder.Property(e => e.Skill).HasColumnName("skill").HasDefaultValue((ushort)0);
        builder.Property(e => e.Tick).HasColumnName("tick");
    }
}
