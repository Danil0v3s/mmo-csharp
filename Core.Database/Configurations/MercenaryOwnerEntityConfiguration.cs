using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MercenaryOwnerEntityConfiguration : IEntityTypeConfiguration<MercenaryOwnerEntity>
{
    public void Configure(EntityTypeBuilder<MercenaryOwnerEntity> builder)
    {
        builder.ToTable("mercenary_owner");
        builder.HasKey(e => e.CharId);
        
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.MercId).HasColumnName("merc_id").HasDefaultValue(0u);
        builder.Property(e => e.ArchCalls).HasColumnName("arch_calls").HasDefaultValue(0);
        builder.Property(e => e.ArchFaith).HasColumnName("arch_faith").HasDefaultValue(0);
        builder.Property(e => e.SpearCalls).HasColumnName("spear_calls").HasDefaultValue(0);
        builder.Property(e => e.SpearFaith).HasColumnName("spear_faith").HasDefaultValue(0);
        builder.Property(e => e.SwordCalls).HasColumnName("sword_calls").HasDefaultValue(0);
        builder.Property(e => e.SwordFaith).HasColumnName("sword_faith").HasDefaultValue(0);
    }
}
