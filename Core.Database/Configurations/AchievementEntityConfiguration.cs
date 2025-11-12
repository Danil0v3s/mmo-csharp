using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AchievementEntityConfiguration : IEntityTypeConfiguration<AchievementEntity>
{
    public void Configure(EntityTypeBuilder<AchievementEntity> builder)
    {
        builder.ToTable("achievement");
        builder.HasKey(e => new { e.CharId, e.Id });
    }
}
