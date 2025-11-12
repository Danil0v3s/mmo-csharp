using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CartInventoryEntityConfiguration : IEntityTypeConfiguration<CartInventoryEntity>
{
    public void Configure(EntityTypeBuilder<CartInventoryEntity> builder)
    {
        builder.ToTable("cart_inventory");
        builder.HasKey(e => e.Id);
    }
}
