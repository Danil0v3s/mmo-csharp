using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GlobalAccRegStrEntityConfiguration : IEntityTypeConfiguration<GlobalAccRegStrEntity>
{
    public void Configure(EntityTypeBuilder<GlobalAccRegStrEntity> builder)
    {
        builder.ToTable("global_acc_reg_str");
        builder.HasKey(e => new { e.AccountId, e.Key, e.Index });
    }
}
