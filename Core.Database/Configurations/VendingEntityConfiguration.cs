using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class VendingEntityConfiguration : IEntityTypeConfiguration<VendingEntity>
{
    public void Configure(EntityTypeBuilder<VendingEntity> builder)
    {
        builder.ToTable("vendings");
        builder.HasKey(e => e.Id);
    }
}
