using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class StorageEntityConfiguration : IEntityTypeConfiguration<StorageEntity>
{
    public void Configure(EntityTypeBuilder<StorageEntity> builder)
    {
        builder.ToTable("storage");
        builder.HasKey(e => e.Id);
    }
}
