using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class VendingItemEntityConfiguration : IEntityTypeConfiguration<VendingItemEntity>
{
    public void Configure(EntityTypeBuilder<VendingItemEntity> builder)
    {
        builder.ToTable("vending_items");
        builder.HasKey(e => new { e.VendingId, e.Index });
        
        builder.Property(e => e.VendingId).HasColumnName("vending_id");
        builder.Property(e => e.Index).HasColumnName("index");
        builder.Property(e => e.CartInventoryId).HasColumnName("cartinventory_id");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Price).HasColumnName("price");
    }
}
