using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildMemberEntityConfiguration : IEntityTypeConfiguration<GuildMemberEntity>
{
    public void Configure(EntityTypeBuilder<GuildMemberEntity> builder)
    {
        builder.ToTable("guild_member");
        builder.HasKey(e => new { e.GuildId, e.CharId });
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Exp).HasColumnName("exp").HasDefaultValue(0ul);
        builder.Property(e => e.Position).HasColumnName("position").HasDefaultValue((byte)0);
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
