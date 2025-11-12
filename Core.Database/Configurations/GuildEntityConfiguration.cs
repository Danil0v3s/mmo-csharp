using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class GuildEntityConfiguration : IEntityTypeConfiguration<GuildEntity>
{
    public void Configure(EntityTypeBuilder<GuildEntity> builder)
    {
        builder.ToTable("guild");
        builder.HasKey(e => e.GuildId);
        
        builder.Property(e => e.GuildId).HasColumnName("guild_id").HasDefaultValue(0u);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0u);
        builder.Property(e => e.Master).HasColumnName("master").HasMaxLength(24).IsRequired().HasDefaultValue("");
        builder.Property(e => e.GuildLv).HasColumnName("guild_lv").HasDefaultValue((byte)0);
        builder.Property(e => e.ConnectMember).HasColumnName("connect_member").HasDefaultValue((byte)0);
        builder.Property(e => e.MaxMember).HasColumnName("max_member").HasDefaultValue((byte)0);
        builder.Property(e => e.AverageLv).HasColumnName("average_lv").HasDefaultValue((ushort)1);
        builder.Property(e => e.Exp).HasColumnName("exp").HasDefaultValue(0ul);
        builder.Property(e => e.NextExp).HasColumnName("next_exp").HasDefaultValue(0ul);
        builder.Property(e => e.SkillPoint).HasColumnName("skill_point").HasDefaultValue((byte)0);
        builder.Property(e => e.Mes1).HasColumnName("mes1").HasMaxLength(60).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Mes2).HasColumnName("mes2").HasMaxLength(120).IsRequired().HasDefaultValue("");
        builder.Property(e => e.EmblemLen).HasColumnName("emblem_len").HasDefaultValue(0u);
        builder.Property(e => e.EmblemId).HasColumnName("emblem_id").HasDefaultValue(0u);
        builder.Property(e => e.EmblemData).HasColumnName("emblem_data");
        builder.Property(e => e.LastMasterChange).HasColumnName("last_master_change");
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
