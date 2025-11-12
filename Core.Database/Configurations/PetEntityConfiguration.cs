using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class PetEntityConfiguration : IEntityTypeConfiguration<PetEntity>
{
    public void Configure(EntityTypeBuilder<PetEntity> builder)
    {
        builder.ToTable("pet");
        builder.HasKey(e => e.PetId);
        
        builder.Property(e => e.PetId).HasColumnName("pet_id");
        builder.Property(e => e.Class).HasColumnName("class").HasDefaultValue(0u);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Level).HasColumnName("level").HasDefaultValue((ushort)0);
        builder.Property(e => e.EggId).HasColumnName("egg_id").HasDefaultValue(0u);
        builder.Property(e => e.Equip).HasColumnName("equip").HasDefaultValue(0u);
        builder.Property(e => e.Intimate).HasColumnName("intimate").HasDefaultValue((ushort)0);
        builder.Property(e => e.Hungry).HasColumnName("hungry").HasDefaultValue((ushort)0);
        builder.Property(e => e.RenameFlag).HasColumnName("rename_flag").HasDefaultValue((byte)0);
        builder.Property(e => e.Incubate).HasColumnName("incubate").HasDefaultValue(0u);
        builder.Property(e => e.Autofeed).HasColumnName("autofeed").HasDefaultValue((short)0);
    }
}
