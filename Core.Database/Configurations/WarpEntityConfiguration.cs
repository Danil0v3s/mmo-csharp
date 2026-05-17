using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class WarpEntityConfiguration : IEntityTypeConfiguration<WarpEntity>
{
    public void Configure(EntityTypeBuilder<WarpEntity> builder)
    {
        builder.ToTable("warp");
        builder.HasKey(e => e.WarpId);

        builder.Property(e => e.WarpId).HasColumnName("warp_id").ValueGeneratedOnAdd();
        builder.Property(e => e.SrcMap).HasColumnName("src_map").HasMaxLength(24).IsRequired();
        builder.Property(e => e.SrcX).HasColumnName("src_x");
        builder.Property(e => e.SrcY).HasColumnName("src_y");
        builder.Property(e => e.SrcDir).HasColumnName("src_dir").HasDefaultValue((byte)0);

        builder.Property(e => e.WarpType).HasColumnName("warp_type").HasMaxLength(8).IsRequired().HasDefaultValue("warp");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();

        builder.Property(e => e.SpanXs).HasColumnName("span_xs").HasDefaultValue((short)0);
        builder.Property(e => e.SpanYs).HasColumnName("span_ys").HasDefaultValue((short)0);

        builder.Property(e => e.DstMap).HasColumnName("dst_map").HasMaxLength(24).IsRequired();
        builder.Property(e => e.DstX).HasColumnName("dst_x");
        builder.Property(e => e.DstY).HasColumnName("dst_y");

        // Spatial lookup index: the map server queries by src_map every
        // time a player walks. Composite (src_map, src_x, src_y) is the
        // natural access pattern.
        builder.HasIndex(e => new { e.SrcMap, e.SrcX, e.SrcY }).HasDatabaseName("ix_warp_src");
    }
}
