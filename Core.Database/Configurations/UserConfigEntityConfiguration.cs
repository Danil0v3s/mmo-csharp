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
        
        builder.Property(e => e.WorldName).HasColumnName("world_name").HasMaxLength(32).IsRequired();
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.Data).HasColumnName("data").HasMaxLength(1024).IsRequired().HasDefaultValue("");
    }
}
