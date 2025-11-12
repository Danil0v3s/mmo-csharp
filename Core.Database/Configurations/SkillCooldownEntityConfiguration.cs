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
        
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Skill).HasColumnName("skill").HasDefaultValue((ushort)0);
        builder.Property(e => e.Tick).HasColumnName("tick");
    }
}
