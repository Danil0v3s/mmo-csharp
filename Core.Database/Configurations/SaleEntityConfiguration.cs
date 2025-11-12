using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class SaleEntityConfiguration : IEntityTypeConfiguration<SaleEntity>
{
    public void Configure(EntityTypeBuilder<SaleEntity> builder)
    {
        builder.ToTable("sales");
        builder.HasKey(e => e.NameId);
        
        builder.Property(e => e.NameId).HasColumnName("nameid");
        builder.Property(e => e.Start).HasColumnName("start");
        builder.Property(e => e.End).HasColumnName("end");
        builder.Property(e => e.Amount).HasColumnName("amount");
    }
}
