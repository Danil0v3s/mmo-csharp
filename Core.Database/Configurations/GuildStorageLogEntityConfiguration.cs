using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildStorageLogEntityConfiguration : IEntityTypeConfiguration<GuildStorageLogEntity>
{
    public void Configure(EntityTypeBuilder<GuildStorageLogEntity> builder)
    {
        builder.ToTable("guild_storage_log");
        builder.HasKey(e => e.Id);
    }
}
