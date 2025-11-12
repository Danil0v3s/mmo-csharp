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
    }
}
