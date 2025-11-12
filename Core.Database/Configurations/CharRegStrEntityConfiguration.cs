using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharRegStrEntityConfiguration : IEntityTypeConfiguration<CharRegStrEntity>
{
    public void Configure(EntityTypeBuilder<CharRegStrEntity> builder)
    {
        builder.ToTable("char_reg_str");
        builder.HasKey(e => new { e.CharId, e.Key, e.Index });
    }
}
