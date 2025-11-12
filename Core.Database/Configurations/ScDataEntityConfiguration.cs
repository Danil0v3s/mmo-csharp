using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ScDataEntityConfiguration : IEntityTypeConfiguration<ScDataEntity>
{
    public void Configure(EntityTypeBuilder<ScDataEntity> builder)
    {
        builder.ToTable("sc_data");
        builder.HasKey(e => new { e.CharId, e.Type });
        
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Type).HasColumnName("type");
        builder.Property(e => e.Tick).HasColumnName("tick");
        builder.Property(e => e.Val1).HasColumnName("val1").HasDefaultValue(0);
        builder.Property(e => e.Val2).HasColumnName("val2").HasDefaultValue(0);
        builder.Property(e => e.Val3).HasColumnName("val3").HasDefaultValue(0);
        builder.Property(e => e.Val4).HasColumnName("val4").HasDefaultValue(0);
    }
}
