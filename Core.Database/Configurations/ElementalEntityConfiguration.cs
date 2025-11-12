using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ElementalEntityConfiguration : IEntityTypeConfiguration<ElementalEntity>
{
    public void Configure(EntityTypeBuilder<ElementalEntity> builder)
    {
        builder.ToTable("elemental");
        builder.HasKey(e => e.EleId);
    }
}
