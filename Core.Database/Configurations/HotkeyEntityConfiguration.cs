using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class HotkeyEntityConfiguration : IEntityTypeConfiguration<HotkeyEntity>
{
    public void Configure(EntityTypeBuilder<HotkeyEntity> builder)
    {
        builder.ToTable("hotkey");
        builder.HasKey(e => new { e.CharId, e.Hotkey });
        
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Hotkey).HasColumnName("hotkey");
        builder.Property(e => e.Type).HasColumnName("type").HasDefaultValue((byte)0);
        builder.Property(e => e.ItemSkillId).HasColumnName("itemskill_id").HasDefaultValue(0u);
        builder.Property(e => e.SkillLvl).HasColumnName("skill_lvl").HasDefaultValue((byte)0);
    }
}
