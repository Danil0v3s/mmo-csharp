using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AuctionEntityConfiguration : IEntityTypeConfiguration<AuctionEntity>
{
    public void Configure(EntityTypeBuilder<AuctionEntity> builder)
    {
        builder.ToTable("auction");
        builder.HasKey(e => e.AuctionId);
    }
}
