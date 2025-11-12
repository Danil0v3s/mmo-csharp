using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ClanAllianceEntityConfiguration : IEntityTypeConfiguration<ClanAllianceEntity>
{
    public void Configure(EntityTypeBuilder<ClanAllianceEntity> builder)
    {
        builder.ToTable("clan_alliance");
        builder.HasKey(e => new { e.ClanId, e.Opposition, e.AllianceId });
        
        builder.Property(e => e.ClanId).HasColumnName("clan_id");
        builder.Property(e => e.Opposition).HasColumnName("opposition");
        builder.Property(e => e.AllianceId).HasColumnName("alliance_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired();
        
        builder.HasIndex(e => e.AllianceId).HasDatabaseName("alliance_id");
        
        // Seed data is applied via DatabaseSeeder from SQL scripts
    }
}
