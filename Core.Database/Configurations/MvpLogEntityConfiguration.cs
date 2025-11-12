using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MvpLogEntityConfiguration : IEntityTypeConfiguration<MvpLogEntity>
{
    public void Configure(EntityTypeBuilder<MvpLogEntity> builder)
    {
        builder.ToTable("mvplog");
        builder.HasKey(e => e.MvpId);
        
        builder.Property(e => e.MvpId).HasColumnName("mvp_id");
        builder.Property(e => e.MvpDate).HasColumnName("mvp_date");
        builder.Property(e => e.KillCharId).HasColumnName("kill_char_id").HasDefaultValue(0);
        builder.Property(e => e.MonsterId).HasColumnName("monster_id").HasDefaultValue((short)0);
        builder.Property(e => e.Prize).HasColumnName("prize").HasDefaultValue(0u);
        builder.Property(e => e.MvpExp).HasColumnName("mvpexp").HasDefaultValue(0ul);
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
    }
}
