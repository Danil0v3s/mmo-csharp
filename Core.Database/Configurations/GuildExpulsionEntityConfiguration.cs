using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildExpulsionEntityConfiguration : IEntityTypeConfiguration<GuildExpulsionEntity>
{
    public void Configure(EntityTypeBuilder<GuildExpulsionEntity> builder)
    {
        builder.ToTable("guild_expulsion");
        builder.HasKey(e => new { e.GuildId, e.Name });
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Mes).HasColumnName("mes").HasMaxLength(40).IsRequired().HasDefaultValue("");
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
    }
}
