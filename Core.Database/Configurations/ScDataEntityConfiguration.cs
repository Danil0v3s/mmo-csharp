using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ScDataEntityConfiguration : IEntityTypeConfiguration<ScDataEntity>
{
    public void Configure(EntityTypeBuilder<ScDataEntity> builder)
    {
        builder.ToTable("sc_data");
        builder.HasKey(e => new { e.CharId, e.Type });
    }
}
