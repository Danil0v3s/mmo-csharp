using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MercenaryEntityConfiguration : IEntityTypeConfiguration<MercenaryEntity>
{
    public void Configure(EntityTypeBuilder<MercenaryEntity> builder)
    {
        builder.ToTable("mercenary");
        builder.HasKey(e => e.MerId);
        
        builder.Property(e => e.MerId).HasColumnName("mer_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Class).HasColumnName("class").HasDefaultValue(0u);
        builder.Property(e => e.Hp).HasColumnName("hp").HasDefaultValue(0u);
        builder.Property(e => e.Sp).HasColumnName("sp").HasDefaultValue(0u);
        builder.Property(e => e.KillCounter).HasColumnName("kill_counter");
        builder.Property(e => e.LifeTime).HasColumnName("life_time").HasDefaultValue(0L);
    }
}
