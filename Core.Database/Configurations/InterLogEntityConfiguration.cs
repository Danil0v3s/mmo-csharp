using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class InterLogEntityConfiguration : IEntityTypeConfiguration<InterLogEntity>
{
    public void Configure(EntityTypeBuilder<InterLogEntity> builder)
    {
        builder.ToTable("interlog");
        builder.HasKey(e => e.Id);
    }
}
