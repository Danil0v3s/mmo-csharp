using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MailEntityConfiguration : IEntityTypeConfiguration<MailEntity>
{
    public void Configure(EntityTypeBuilder<MailEntity> builder)
    {
        builder.ToTable("mail");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SendName).HasColumnName("send_name").HasMaxLength(30).IsRequired().HasDefaultValue("");
        builder.Property(e => e.SendId).HasColumnName("send_id").HasDefaultValue(0u);
        builder.Property(e => e.DestName).HasColumnName("dest_name").HasMaxLength(30).IsRequired().HasDefaultValue("");
        builder.Property(e => e.DestId).HasColumnName("dest_id").HasDefaultValue(0u);
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(45).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(500).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Time).HasColumnName("time").HasDefaultValue(0u);
        builder.Property(e => e.Status).HasColumnName("status").HasDefaultValue((short)0);
        builder.Property(e => e.Zeny).HasColumnName("zeny").HasDefaultValue(0u);
        builder.Property(e => e.Type).HasColumnName("type").HasDefaultValue((short)0);
    }
}
