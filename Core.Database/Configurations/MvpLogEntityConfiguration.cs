using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MvpLogEntityConfiguration : IEntityTypeConfiguration<MvpLogEntity>
{
    public void Configure(EntityTypeBuilder<MvpLogEntity> builder)
    {
        builder.ToTable("mvplog");
        builder.HasKey(e => e.MvpId);
    }
}
