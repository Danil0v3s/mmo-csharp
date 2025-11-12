using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharLogEntityConfiguration : IEntityTypeConfiguration<CharLogEntity>
{
    public void Configure(EntityTypeBuilder<CharLogEntity> builder)
    {
        builder.ToTable("charlog");
        builder.HasKey(e => e.Id);
    }
}
