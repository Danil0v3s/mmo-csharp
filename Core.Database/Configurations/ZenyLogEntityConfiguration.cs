using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ZenyLogEntityConfiguration : IEntityTypeConfiguration<ZenyLogEntity>
{
    public void Configure(EntityTypeBuilder<ZenyLogEntity> builder)
    {
        builder.ToTable("zenylog");
        builder.HasKey(e => e.Id);
    }
}
