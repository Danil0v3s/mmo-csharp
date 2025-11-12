using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class IpBanListEntityConfiguration : IEntityTypeConfiguration<IpBanListEntity>
{
    public void Configure(EntityTypeBuilder<IpBanListEntity> builder)
    {
        builder.ToTable("ipbanlist");
        builder.HasKey(e => new { e.List, e.BTime });
        
        builder.Property(e => e.List).HasColumnName("list").HasMaxLength(15).IsRequired().HasDefaultValue("");
        builder.Property(e => e.BTime).HasColumnName("btime");
        builder.Property(e => e.RTime).HasColumnName("rtime");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(255).IsRequired().HasDefaultValue("");
    }
}
