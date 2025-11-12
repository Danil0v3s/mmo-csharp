using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AccRegStrEntityConfiguration : IEntityTypeConfiguration<AccRegStrEntity>
{
    public void Configure(EntityTypeBuilder<AccRegStrEntity> builder)
    {
        builder.ToTable("acc_reg_str");
        builder.HasKey(e => new { e.AccountId, e.Key, e.Index });
    }
}
