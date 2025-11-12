using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildCastleEntityConfiguration : IEntityTypeConfiguration<GuildCastleEntity>
{
    public void Configure(EntityTypeBuilder<GuildCastleEntity> builder)
    {
        builder.ToTable("guild_castle");
        builder.HasKey(e => e.CastleId);
        
        builder.Property(e => e.CastleId).HasColumnName("castle_id").HasDefaultValue(0u);
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.Economy).HasColumnName("economy").HasDefaultValue(0u);
        builder.Property(e => e.Defense).HasColumnName("defense").HasDefaultValue(0u);
        builder.Property(e => e.TriggerE).HasColumnName("triggerE").HasDefaultValue(0u);
        builder.Property(e => e.TriggerD).HasColumnName("triggerD").HasDefaultValue(0u);
        builder.Property(e => e.NextTime).HasColumnName("nextTime").HasDefaultValue(0u);
        builder.Property(e => e.PayTime).HasColumnName("payTime").HasDefaultValue(0u);
        builder.Property(e => e.CreateTime).HasColumnName("createTime").HasDefaultValue(0u);
        builder.Property(e => e.VisibleC).HasColumnName("visibleC").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG0).HasColumnName("visibleG0").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG1).HasColumnName("visibleG1").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG2).HasColumnName("visibleG2").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG3).HasColumnName("visibleG3").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG4).HasColumnName("visibleG4").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG5).HasColumnName("visibleG5").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG6).HasColumnName("visibleG6").HasDefaultValue(0u);
        builder.Property(e => e.VisibleG7).HasColumnName("visibleG7").HasDefaultValue(0u);
        
        builder.HasIndex(e => e.GuildId).HasDatabaseName("guild_id");
    }
}
