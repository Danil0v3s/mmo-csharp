using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class QuestEntityConfiguration : IEntityTypeConfiguration<QuestEntity>
{
    public void Configure(EntityTypeBuilder<QuestEntity> builder)
    {
        builder.ToTable("quest");
        builder.HasKey(e => new { e.CharId, e.QuestId });
        
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.QuestId).HasColumnName("quest_id");
        builder.Property(e => e.State).HasColumnName("state").HasConversion<string>().IsRequired().HasDefaultValue("0");
        builder.Property(e => e.Time).HasColumnName("time").HasDefaultValue(0u);
        builder.Property(e => e.Count1).HasColumnName("count1").HasDefaultValue(0u);
        builder.Property(e => e.Count2).HasColumnName("count2").HasDefaultValue(0u);
        builder.Property(e => e.Count3).HasColumnName("count3").HasDefaultValue(0u);
    }
}
