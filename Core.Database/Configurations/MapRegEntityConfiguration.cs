using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MapRegEntityConfiguration : IEntityTypeConfiguration<MapRegEntity>
{
    public void Configure(EntityTypeBuilder<MapRegEntity> builder)
    {
        builder.ToTable("mapreg");
        builder.HasKey(e => new { e.VarName, e.Index });
    }
}
