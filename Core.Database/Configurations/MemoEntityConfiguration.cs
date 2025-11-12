using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MemoEntityConfiguration : IEntityTypeConfiguration<MemoEntity>
{
    public void Configure(EntityTypeBuilder<MemoEntity> builder)
    {
        builder.ToTable("memo");
        builder.HasKey(e => e.MemoId);
    }
}
