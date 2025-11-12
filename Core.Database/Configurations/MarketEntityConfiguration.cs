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
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired().HasDefaultValue("");
        builder.Property(e => e.NameId).HasColumnName("nameid");
        builder.Property(e => e.Price).HasColumnName("price");
        builder.Property(e => e.Amount).HasColumnName("amount");
        builder.Property(e => e.Flag).HasColumnName("flag").HasDefaultValue((byte)0);
    }
}
