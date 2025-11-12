using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SkillEntityConfiguration : IEntityTypeConfiguration<SkillEntity>
{
    public void Configure(EntityTypeBuilder<SkillEntity> builder)
    {
        builder.ToTable("skill");
        builder.HasKey(e => new { e.CharId, e.Id });
        
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValue((ushort)0);
        builder.Property(e => e.Lv).HasColumnName("lv").HasDefaultValue((byte)0);
        builder.Property(e => e.Flag).HasColumnName("flag").HasDefaultValue((byte)0);
    }
}
