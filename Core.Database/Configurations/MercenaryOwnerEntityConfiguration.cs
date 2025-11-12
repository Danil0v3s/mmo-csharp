using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MercenaryOwnerEntityConfiguration : IEntityTypeConfiguration<MercenaryOwnerEntity>
{
    public void Configure(EntityTypeBuilder<MercenaryOwnerEntity> builder)
    {
        builder.ToTable("mercenary_owner");
        builder.HasKey(e => e.CharId);
    }
}
