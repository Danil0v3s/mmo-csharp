using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MercenaryEntityConfiguration : IEntityTypeConfiguration<MercenaryEntity>
{
    public void Configure(EntityTypeBuilder<MercenaryEntity> builder)
    {
        builder.ToTable("mercenary");
        builder.HasKey(e => e.MerId);
    }
}
