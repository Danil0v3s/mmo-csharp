using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CashLogEntityConfiguration : IEntityTypeConfiguration<CashLogEntity>
{
    public void Configure(EntityTypeBuilder<CashLogEntity> builder)
    {
        builder.ToTable("cashlog");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0);
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasDefaultValue("S");
        builder.Property(e => e.CashType).HasColumnName("cash_type").HasConversion<string>().IsRequired().HasDefaultValue("O");
        builder.Property(e => e.Amount).HasColumnName("amount").HasDefaultValue(0);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.Type).HasDatabaseName("type");
    }
}
