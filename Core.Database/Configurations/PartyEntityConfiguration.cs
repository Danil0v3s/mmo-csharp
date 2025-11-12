using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class PartyEntityConfiguration : IEntityTypeConfiguration<PartyEntity>
{
    public void Configure(EntityTypeBuilder<PartyEntity> builder)
    {
        builder.ToTable("party");
        builder.HasKey(e => e.PartyId);
        
        builder.Property(e => e.PartyId).HasColumnName("party_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Exp).HasColumnName("exp").HasDefaultValue((byte)0);
        builder.Property(e => e.Item).HasColumnName("item").HasDefaultValue((byte)0);
        builder.Property(e => e.LeaderId).HasColumnName("leader_id").HasDefaultValue(0u);
        builder.Property(e => e.LeaderChar).HasColumnName("leader_char").HasDefaultValue(0u);
    }
}
