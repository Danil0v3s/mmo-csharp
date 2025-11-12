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
    }
}
