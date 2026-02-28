using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AccountStoragePayloadEntityConfiguration : IEntityTypeConfiguration<AccountStoragePayloadEntity>
{
    public void Configure(EntityTypeBuilder<AccountStoragePayloadEntity> builder)
    {
        builder.ToTable("account_storage_payload");
        builder.HasKey(e => e.AccountId);

        builder.Property(e => e.AccountId).ValueGeneratedNever().HasColumnName("account_id");
        builder.Property(e => e.Data).HasColumnName("data").HasColumnType("longblob");
    }
}
