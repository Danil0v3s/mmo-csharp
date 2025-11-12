using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class FriendEntityConfiguration : IEntityTypeConfiguration<FriendEntity>
{
    public void Configure(EntityTypeBuilder<FriendEntity> builder)
    {
        builder.ToTable("friends");
        builder.HasKey(e => new { e.CharId, e.FriendId });
    }
}
