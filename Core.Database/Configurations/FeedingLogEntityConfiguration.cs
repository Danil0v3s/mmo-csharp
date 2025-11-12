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
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.TargetId).HasColumnName("target_id");
        builder.Property(e => e.TargetClass).HasColumnName("target_class");
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().IsRequired();
        builder.Property(e => e.Intimacy).HasColumnName("intimacy");
        builder.Property(e => e.ItemId).HasColumnName("item_id");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired();
        builder.Property(e => e.X).HasColumnName("x");
        builder.Property(e => e.Y).HasColumnName("y");
    }
}
