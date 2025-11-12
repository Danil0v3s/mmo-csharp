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
        
        builder.Property(e => e.HomunId).HasColumnName("homun_id");
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValue(0);
        builder.Property(e => e.Lv).HasColumnName("lv");
    }
}
