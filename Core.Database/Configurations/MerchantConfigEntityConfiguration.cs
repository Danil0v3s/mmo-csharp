using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MerchantConfigEntityConfiguration : IEntityTypeConfiguration<MerchantConfigEntity>
{
    public void Configure(EntityTypeBuilder<MerchantConfigEntity> builder)
    {
        builder.ToTable("merchant_configs");
        builder.HasKey(e => new { e.WorldName, e.AccountId, e.CharId, e.StoreType });
        
        builder.Property(e => e.WorldName).HasColumnName("world_name").HasMaxLength(32).IsRequired();
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.StoreType).HasColumnName("store_type");
        builder.Property(e => e.Data).HasColumnName("data").HasMaxLength(1024).IsRequired().HasDefaultValue("");
    }
}
