using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BonusScriptEntityConfiguration : IEntityTypeConfiguration<BonusScriptEntity>
{
    public void Configure(EntityTypeBuilder<BonusScriptEntity> builder)
    {
        builder.ToTable("bonus_script");
        builder.HasNoKey();
        
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Script).HasColumnName("script").HasColumnType("text").IsRequired();
        builder.Property(e => e.Tick).HasColumnName("tick").HasDefaultValue(0L);
        builder.Property(e => e.Flag).HasColumnName("flag").HasDefaultValue((ushort)0);
        builder.Property(e => e.Type).HasColumnName("type").HasDefaultValue((byte)0);
        builder.Property(e => e.Icon).HasColumnName("icon").HasDefaultValue((short)-1);
        
        builder.HasIndex(e => e.CharId).HasDatabaseName("char_id");
    }
}
