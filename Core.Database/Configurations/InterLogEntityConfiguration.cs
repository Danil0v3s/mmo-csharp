using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class InterLogEntityConfiguration : IEntityTypeConfiguration<InterLogEntity>
{
    public void Configure(EntityTypeBuilder<InterLogEntity> builder)
    {
        builder.ToTable("interlog");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.Log).HasColumnName("log").HasMaxLength(255).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.Time).HasDatabaseName("time");
    }
}
