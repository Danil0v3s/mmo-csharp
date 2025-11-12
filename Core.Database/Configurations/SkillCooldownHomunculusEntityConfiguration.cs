using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillCooldownHomunculusEntityConfiguration : IEntityTypeConfiguration<SkillCooldownHomunculusEntity>
{
    public void Configure(EntityTypeBuilder<SkillCooldownHomunculusEntity> builder)
    {
        builder.ToTable("skillcooldown_homunculus");
        builder.HasKey(e => new { e.HomunId, e.Skill });
    }
}
