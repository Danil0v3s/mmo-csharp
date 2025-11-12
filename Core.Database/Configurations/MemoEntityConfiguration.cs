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
        
        builder.Property(e => e.MemoId).HasColumnName("memo_id");
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        builder.Property(e => e.X).HasColumnName("x").HasDefaultValue((ushort)0);
        builder.Property(e => e.Y).HasColumnName("y").HasDefaultValue((ushort)0);
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
