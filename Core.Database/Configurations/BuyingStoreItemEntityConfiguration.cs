using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BuyingStoreItemEntityConfiguration : IEntityTypeConfiguration<BuyingStoreItemEntity>
{
    public void Configure(EntityTypeBuilder<BuyingStoreItemEntity> builder)
    {
        builder.ToTable("buyingstore_items");
        builder.HasKey(e => new { e.BuyingStoreId, e.Index });
    }
}
