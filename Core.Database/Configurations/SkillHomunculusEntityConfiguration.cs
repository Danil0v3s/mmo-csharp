using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillHomunculusEntityConfiguration : IEntityTypeConfiguration<SkillHomunculusEntity>
{
    public void Configure(EntityTypeBuilder<SkillHomunculusEntity> builder)
    {
        builder.ToTable("skill_homunculus");
        builder.HasKey(e => new { e.HomunId, e.Id });
    }
}
