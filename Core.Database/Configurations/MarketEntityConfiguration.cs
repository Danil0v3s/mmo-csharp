using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MarketEntityConfiguration : IEntityTypeConfiguration<MarketEntity>
{
    public void Configure(EntityTypeBuilder<MarketEntity> builder)
    {
        builder.ToTable("market");
        builder.HasKey(e => new { e.Name, e.NameId });
    }
}
