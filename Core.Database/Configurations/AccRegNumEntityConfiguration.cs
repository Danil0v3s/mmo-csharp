using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AccRegNumEntityConfiguration : IEntityTypeConfiguration<AccRegNumEntity>
{
    public void Configure(EntityTypeBuilder<AccRegNumEntity> builder)
    {
        builder.ToTable("acc_reg_num");
        builder.HasKey(e => new { e.AccountId, e.Key, e.Index });
        
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(32).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Index).HasColumnName("index").HasDefaultValue(0u);
        builder.Property(e => e.Value).HasColumnName("value").HasDefaultValue(0L);
        
        builder.HasIndex(e => e.AccountId).HasDatabaseName("account_id");
    }
}
