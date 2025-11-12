using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ChatLogEntityConfiguration : IEntityTypeConfiguration<ChatLogEntity>
{
    public void Configure(EntityTypeBuilder<ChatLogEntity> builder)
    {
        builder.ToTable("chatlog");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasDefaultValue("O");
        builder.Property(e => e.TypeId).HasColumnName("type_id").HasDefaultValue(0);
        builder.Property(e => e.SrcCharId).HasColumnName("src_charid").HasDefaultValue(0);
        builder.Property(e => e.SrcAccountId).HasColumnName("src_accountid").HasDefaultValue(0);
        builder.Property(e => e.SrcMap).HasColumnName("src_map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        builder.Property(e => e.SrcMapX).HasColumnName("src_map_x").HasDefaultValue((short)0);
        builder.Property(e => e.SrcMapY).HasColumnName("src_map_y").HasDefaultValue((short)0);
        builder.Property(e => e.DstCharName).HasColumnName("dst_charname").HasMaxLength(25).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(150).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.SrcAccountId);
        builder.HasIndex(e => e.SrcCharId);
    }
}
