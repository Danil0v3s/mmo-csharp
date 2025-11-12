using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class PartyBookingEntityConfiguration : IEntityTypeConfiguration<PartyBookingEntity>
{
    public void Configure(EntityTypeBuilder<PartyBookingEntity> builder)
    {
        builder.ToTable("party_bookings");
        builder.HasKey(e => new { e.WorldName, e.AccountId, e.CharId });
    }
}
