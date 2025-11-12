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
    }
}
