using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AccRegNumEntityConfiguration : IEntityTypeConfiguration<AccRegNumEntity>
{
    public void Configure(EntityTypeBuilder<AccRegNumEntity> builder)
    {
        builder.ToTable("acc_reg_num");
        builder.HasKey(e => new { e.AccountId, e.Key, e.Index });
    }
}
