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
        
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Count1).HasColumnName("count1").HasDefaultValue(0u);
        builder.Property(e => e.Count2).HasColumnName("count2").HasDefaultValue(0u);
        builder.Property(e => e.Count3).HasColumnName("count3").HasDefaultValue(0u);
        builder.Property(e => e.Count4).HasColumnName("count4").HasDefaultValue(0u);
        builder.Property(e => e.Count5).HasColumnName("count5").HasDefaultValue(0u);
        builder.Property(e => e.Count6).HasColumnName("count6").HasDefaultValue(0u);
        builder.Property(e => e.Count7).HasColumnName("count7").HasDefaultValue(0u);
        builder.Property(e => e.Count8).HasColumnName("count8").HasDefaultValue(0u);
        builder.Property(e => e.Count9).HasColumnName("count9").HasDefaultValue(0u);
        builder.Property(e => e.Count10).HasColumnName("count10").HasDefaultValue(0u);
        builder.Property(e => e.Completed).HasColumnName("completed");
        builder.Property(e => e.Rewarded).HasColumnName("rewarded");
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
