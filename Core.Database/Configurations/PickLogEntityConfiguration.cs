using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class PickLogEntityConfiguration : IEntityTypeConfiguration<PickLogEntity>
{
    public void Configure(EntityTypeBuilder<PickLogEntity> builder)
    {
        builder.ToTable("picklog");
        builder.HasKey(e => e.Id);
    }
}
