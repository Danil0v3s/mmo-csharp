using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharRegNumEntityConfiguration : IEntityTypeConfiguration<CharRegNumEntity>
{
    public void Configure(EntityTypeBuilder<CharRegNumEntity> builder)
    {
        builder.ToTable("char_reg_num");
        builder.HasKey(e => new { e.CharId, e.Key, e.Index });
    }
}
