using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class LoginLogEntityConfiguration : IEntityTypeConfiguration<LoginLogEntity>
{
    public void Configure(EntityTypeBuilder<LoginLogEntity> builder)
    {
        builder.ToTable("loginlog");
        builder.HasNoKey();
        
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.Ip).HasColumnName("ip").HasMaxLength(15).IsRequired().HasDefaultValue("");
        builder.Property(e => e.User).HasColumnName("user").HasMaxLength(23).IsRequired().HasDefaultValue("");
        builder.Property(e => e.RCode).HasColumnName("rcode").HasDefaultValue((sbyte)0);
        builder.Property(e => e.Log).HasColumnName("log").HasMaxLength(255).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.Ip);
    }
}
