using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ChatLogEntityConfiguration : IEntityTypeConfiguration<ChatLogEntity>
{
    public void Configure(EntityTypeBuilder<ChatLogEntity> builder)
    {
        builder.ToTable("chatlog");
        builder.HasKey(e => e.Id);
    }
}
