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
    }
}
