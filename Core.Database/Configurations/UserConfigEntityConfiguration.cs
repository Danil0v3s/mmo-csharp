using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class UserConfigEntityConfiguration : IEntityTypeConfiguration<UserConfigEntity>
{
    public void Configure(EntityTypeBuilder<UserConfigEntity> builder)
    {
        builder.ToTable("user_configs");
        builder.HasKey(e => new { e.WorldName, e.AccountId });
    }
}
