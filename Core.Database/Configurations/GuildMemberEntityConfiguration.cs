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
    }
}
