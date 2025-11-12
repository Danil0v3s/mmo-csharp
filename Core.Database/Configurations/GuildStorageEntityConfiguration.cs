using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildStorageEntityConfiguration : IEntityTypeConfiguration<GuildStorageEntity>
{
    public void Configure(EntityTypeBuilder<GuildStorageEntity> builder)
    {
        builder.ToTable("guild_storage");
        builder.HasKey(e => e.Id);
    }
}
