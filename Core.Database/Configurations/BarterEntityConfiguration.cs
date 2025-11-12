using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BarterEntityConfiguration : IEntityTypeConfiguration<BarterEntity>
{
    public void Configure(EntityTypeBuilder<BarterEntity> builder)
    {
        builder.ToTable("barter");
        builder.HasKey(e => new { e.Name, e.Index });
    }
}
