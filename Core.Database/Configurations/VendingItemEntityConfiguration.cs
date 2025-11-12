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
    }
}
