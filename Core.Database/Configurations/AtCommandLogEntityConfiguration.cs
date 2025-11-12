using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AtCommandLogEntityConfiguration : IEntityTypeConfiguration<AtCommandLogEntity>
{
    public void Configure(EntityTypeBuilder<AtCommandLogEntity> builder)
    {
        builder.ToTable("atcommandlog");
        builder.HasKey(e => e.AtCommandId);
    }
}
