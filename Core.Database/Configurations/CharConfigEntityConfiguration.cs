using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharConfigEntityConfiguration : IEntityTypeConfiguration<CharConfigEntity>
{
    public void Configure(EntityTypeBuilder<CharConfigEntity> builder)
    {
        builder.ToTable("char_configs");
        builder.HasKey(e => new { e.WorldName, e.AccountId, e.CharId });
    }
}
