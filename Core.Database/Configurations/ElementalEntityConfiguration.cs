using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ElementalEntityConfiguration : IEntityTypeConfiguration<ElementalEntity>
{
    public void Configure(EntityTypeBuilder<ElementalEntity> builder)
    {
        builder.ToTable("elemental");
        builder.HasKey(e => e.EleId);
        
        builder.Property(e => e.EleId).HasColumnName("ele_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Class).HasColumnName("class").HasDefaultValue(0u);
        builder.Property(e => e.Mode).HasColumnName("mode").HasDefaultValue(1u);
        builder.Property(e => e.Hp).HasColumnName("hp").HasDefaultValue(0u);
        builder.Property(e => e.Sp).HasColumnName("sp").HasDefaultValue(0u);
        builder.Property(e => e.MaxHp).HasColumnName("max_hp").HasDefaultValue(0u);
        builder.Property(e => e.MaxSp).HasColumnName("max_sp").HasDefaultValue(0u);
        builder.Property(e => e.Atk1).HasColumnName("atk1").HasDefaultValue(0u);
        builder.Property(e => e.Atk2).HasColumnName("atk2").HasDefaultValue(0u);
        builder.Property(e => e.Matk).HasColumnName("matk").HasDefaultValue(0u);
        builder.Property(e => e.Aspd).HasColumnName("aspd").HasDefaultValue((ushort)0);
        builder.Property(e => e.Def).HasColumnName("def").HasDefaultValue((ushort)0);
        builder.Property(e => e.Mdef).HasColumnName("mdef").HasDefaultValue((ushort)0);
        builder.Property(e => e.Flee).HasColumnName("flee").HasDefaultValue((ushort)0);
        builder.Property(e => e.Hit).HasColumnName("hit").HasDefaultValue((ushort)0);
        builder.Property(e => e.LifeTime).HasColumnName("life_time").HasDefaultValue(0L);
    }
}
