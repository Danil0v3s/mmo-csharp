using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharRegStrEntityConfiguration : IEntityTypeConfiguration<CharRegStrEntity>
{
    public void Configure(EntityTypeBuilder<CharRegStrEntity> builder)
    {
        builder.ToTable("char_reg_str");
        builder.HasKey(e => new { e.CharId, e.Key, e.Index });
        
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(32).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Index).HasColumnName("index").HasDefaultValue(0u);
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(254).IsRequired().HasDefaultValue("0");
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
