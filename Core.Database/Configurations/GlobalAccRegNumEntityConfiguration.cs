using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GlobalAccRegNumEntityConfiguration : IEntityTypeConfiguration<GlobalAccRegNumEntity>
{
    public void Configure(EntityTypeBuilder<GlobalAccRegNumEntity> builder)
    {
        builder.ToTable("global_acc_reg_num");
        builder.HasKey(e => new { e.AccountId, e.Key, e.Index });
    }
}
