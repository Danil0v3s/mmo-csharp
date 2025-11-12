using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class FeedingLogEntityConfiguration : IEntityTypeConfiguration<FeedingLogEntity>
{
    public void Configure(EntityTypeBuilder<FeedingLogEntity> builder)
    {
        builder.ToTable("feedinglog");
        builder.HasKey(e => e.Id);
    }
}
