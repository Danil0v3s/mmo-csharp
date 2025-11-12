using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class HomunculusEntityConfiguration : IEntityTypeConfiguration<HomunculusEntity>
{
    public void Configure(EntityTypeBuilder<HomunculusEntity> builder)
    {
        builder.ToTable("homunculus");
        builder.HasKey(e => e.HomunId);
        
        builder.Property(e => e.HomunId).HasColumnName("homun_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Class).HasColumnName("class").HasDefaultValue(0u);
        builder.Property(e => e.PrevClass).HasColumnName("prev_class").HasDefaultValue(0);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Level).HasColumnName("level").HasDefaultValue((short)0);
        builder.Property(e => e.Exp).HasColumnName("exp").HasDefaultValue(0ul);
        builder.Property(e => e.Intimacy).HasColumnName("intimacy").HasDefaultValue(0);
        builder.Property(e => e.Hunger).HasColumnName("hunger").HasDefaultValue((short)0);
        builder.Property(e => e.Str).HasColumnName("str").HasDefaultValue((ushort)0);
        builder.Property(e => e.Agi).HasColumnName("agi").HasDefaultValue((ushort)0);
        builder.Property(e => e.Vit).HasColumnName("vit").HasDefaultValue((ushort)0);
        builder.Property(e => e.Int).HasColumnName("int").HasDefaultValue((ushort)0);
        builder.Property(e => e.Dex).HasColumnName("dex").HasDefaultValue((ushort)0);
        builder.Property(e => e.Luk).HasColumnName("luk").HasDefaultValue((ushort)0);
        builder.Property(e => e.Hp).HasColumnName("hp").HasDefaultValue(0u);
        builder.Property(e => e.MaxHp).HasColumnName("max_hp").HasDefaultValue(0u);
        builder.Property(e => e.Sp).HasColumnName("sp").HasDefaultValue(0u);
        builder.Property(e => e.MaxSp).HasColumnName("max_sp").HasDefaultValue(0u);
        builder.Property(e => e.SkillPoint).HasColumnName("skill_point").HasDefaultValue((ushort)0);
        builder.Property(e => e.Alive).HasColumnName("alive").HasDefaultValue((short)1);
        builder.Property(e => e.RenameFlag).HasColumnName("rename_flag").HasDefaultValue((short)0);
        builder.Property(e => e.Vaporize).HasColumnName("vaporize").HasDefaultValue((short)0);
        builder.Property(e => e.Autofeed).HasColumnName("autofeed").HasDefaultValue((short)0);
    }
}
