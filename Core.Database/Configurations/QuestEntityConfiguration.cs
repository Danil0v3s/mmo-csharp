using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class QuestEntityConfiguration : IEntityTypeConfiguration<QuestEntity>
{
    public void Configure(EntityTypeBuilder<QuestEntity> builder)
    {
        builder.ToTable("quest");
        builder.HasKey(e => new { e.CharId, e.QuestId });
    }
}
