using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildStoragePayloadEntityConfiguration : IEntityTypeConfiguration<GuildStoragePayloadEntity>
{
    public void Configure(EntityTypeBuilder<GuildStoragePayloadEntity> builder)
    {
        builder.ToTable("guild_storage_payload");
        builder.HasKey(e => e.GuildId);

        builder.Property(e => e.GuildId).ValueGeneratedNever().HasColumnName("guild_id");
        builder.Property(e => e.Data).HasColumnName("data").HasColumnType("longblob");
    }
}
