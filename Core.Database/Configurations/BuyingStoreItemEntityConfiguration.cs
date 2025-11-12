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
        
        builder.Property(e => e.BuyingStoreId).HasColumnName("buyingstore_id");
        builder.Property(e => e.Index).HasColumnName("index");
        builder.Property(e => e.ItemId).HasColumnName("item_id");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Price).HasColumnName("price");
    }
}
