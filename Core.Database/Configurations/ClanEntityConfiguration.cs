using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ClanEntityConfiguration : IEntityTypeConfiguration<ClanEntity>
{
    public void Configure(EntityTypeBuilder<ClanEntity> builder)
    {
        builder.ToTable("clan");
        builder.HasKey(e => e.ClanId);
        
        builder.Property(e => e.ClanId).HasColumnName("clan_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired();
        builder.Property(e => e.Master).HasColumnName("master").HasMaxLength(24).IsRequired();
        builder.Property(e => e.MapName).HasColumnName("mapname").HasMaxLength(24).IsRequired();
        builder.Property(e => e.MaxMember).HasColumnName("max_member");
        
        // Seed data is applied via DatabaseSeeder from SQL scripts
    }
}
