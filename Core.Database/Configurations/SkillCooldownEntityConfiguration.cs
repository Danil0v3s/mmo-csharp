using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillCooldownEntityConfiguration : IEntityTypeConfiguration<SkillCooldownEntity>
{
    public void Configure(EntityTypeBuilder<SkillCooldownEntity> builder)
    {
        builder.ToTable("skillcooldown");
        builder.HasKey(e => new { e.CharId, e.Skill });
    }
}
