using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BuyingStoreEntityConfiguration : IEntityTypeConfiguration<BuyingStoreEntity>
{
    public void Configure(EntityTypeBuilder<BuyingStoreEntity> builder)
    {
        builder.ToTable("buyingstores");
        builder.HasKey(e => e.Id);
    }
}
