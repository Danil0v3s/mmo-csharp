using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class HomunculusEntityConfiguration : IEntityTypeConfiguration<HomunculusEntity>
{
    public void Configure(EntityTypeBuilder<HomunculusEntity> builder)
    {
        builder.ToTable("homunculus");
        builder.HasKey(e => e.HomunId);
    }
}
