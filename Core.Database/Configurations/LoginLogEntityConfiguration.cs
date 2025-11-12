using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class LoginLogEntityConfiguration : IEntityTypeConfiguration<LoginLogEntity>
{
    public void Configure(EntityTypeBuilder<LoginLogEntity> builder)
    {
        builder.ToTable("loginlog");
        builder.HasKey(e => new { e.Time, e.Ip, e.User });
    }
}
