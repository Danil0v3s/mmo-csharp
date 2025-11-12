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
        
        builder.Property(e => e.VarName).HasColumnName("varname").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Index).HasColumnName("index").HasDefaultValue(0u);
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(255).IsRequired();
    }
}
