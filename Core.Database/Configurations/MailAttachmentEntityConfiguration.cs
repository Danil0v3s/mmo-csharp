using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MailAttachmentEntityConfiguration : IEntityTypeConfiguration<MailAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<MailAttachmentEntity> builder)
    {
        builder.ToTable("mail_attachments");
        builder.HasKey(e => new { e.Id, e.Index });
    }
}
