using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class AtCommandLogEntityConfiguration : IEntityTypeConfiguration<AtCommandLogEntity>
{
    public void Configure(EntityTypeBuilder<AtCommandLogEntity> builder)
    {
        builder.ToTable("atcommandlog");
        builder.HasKey(e => e.AtCommandId);
        
        builder.Property(e => e.AtCommandId).HasColumnName("atcommand_id");
        builder.Property(e => e.AtCommandDate).HasColumnName("atcommand_date");
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.CharName).HasColumnName("char_name").HasMaxLength(25).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Command).HasColumnName("command").HasMaxLength(255).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.CharId);
    }
}
