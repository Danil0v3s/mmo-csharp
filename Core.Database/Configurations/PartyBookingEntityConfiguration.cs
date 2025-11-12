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
        
        builder.Property(e => e.WorldName).HasColumnName("world_name").HasMaxLength(32).IsRequired();
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.CharName).HasColumnName("char_name").HasMaxLength(24).IsRequired();
        builder.Property(e => e.Purpose).HasColumnName("purpose").HasDefaultValue((ushort)0);
        builder.Property(e => e.Assist).HasColumnName("assist").HasDefaultValue((byte)0);
        builder.Property(e => e.DamageDealer).HasColumnName("damagedealer").HasDefaultValue((byte)0);
        builder.Property(e => e.Healer).HasColumnName("healer").HasDefaultValue((byte)0);
        builder.Property(e => e.Tanker).HasColumnName("tanker").HasDefaultValue((byte)0);
        builder.Property(e => e.MinimumLevel).HasColumnName("minimum_level").HasDefaultValue((ushort)0);
        builder.Property(e => e.MaximumLevel).HasColumnName("maximum_level").HasDefaultValue((ushort)0);
        builder.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(255).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Created).HasColumnName("created");
    }
}
