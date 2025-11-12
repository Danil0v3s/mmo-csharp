using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class NpcLogEntityConfiguration : IEntityTypeConfiguration<NpcLogEntity>
{
    public void Configure(EntityTypeBuilder<NpcLogEntity> builder)
    {
        builder.ToTable("npclog");
        builder.HasKey(e => e.NpcId);
        
        builder.Property(e => e.NpcId).HasColumnName("npc_id");
        builder.Property(e => e.NpcDate).HasColumnName("npc_date");
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.CharName).HasColumnName("char_name").HasMaxLength(25).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Mes).HasColumnName("mes").HasMaxLength(255).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.CharId);
    }
}
