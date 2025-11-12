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
        
        builder.Property(e => e.HomunId).HasColumnName("homun_id");
        builder.Property(e => e.Skill).HasColumnName("skill").HasDefaultValue((ushort)0);
        builder.Property(e => e.Tick).HasColumnName("tick");
    }
}
