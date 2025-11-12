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
    }
}
