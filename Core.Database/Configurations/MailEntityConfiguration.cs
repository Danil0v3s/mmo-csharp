using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MailEntityConfiguration : IEntityTypeConfiguration<MailEntity>
{
    public void Configure(EntityTypeBuilder<MailEntity> builder)
    {
        builder.ToTable("mail");
        builder.HasKey(e => e.Id);
    }
}
