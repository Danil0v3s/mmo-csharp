using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharLogEntityConfiguration : IEntityTypeConfiguration<CharLogEntity>
{
    public void Configure(EntityTypeBuilder<CharLogEntity> builder)
    {
        builder.ToTable("charlog");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Time).HasColumnName("time");
        builder.Property(e => e.CharMsg).HasColumnName("char_msg").HasMaxLength(255).IsRequired().HasDefaultValue("char select");
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0u);
        builder.Property(e => e.CharNum).HasColumnName("char_num").HasDefaultValue((byte)0);
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(23).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Str).HasColumnName("str").HasDefaultValue(0u);
        builder.Property(e => e.Agi).HasColumnName("agi").HasDefaultValue(0u);
        builder.Property(e => e.Vit).HasColumnName("vit").HasDefaultValue(0u);
        builder.Property(e => e.Int).HasColumnName("int").HasDefaultValue(0u);
        builder.Property(e => e.Dex).HasColumnName("dex").HasDefaultValue(0u);
        builder.Property(e => e.Luk).HasColumnName("luk").HasDefaultValue(0u);
        builder.Property(e => e.Hair).HasColumnName("hair").HasDefaultValue((sbyte)0);
        builder.Property(e => e.HairColor).HasColumnName("hair_color").HasDefaultValue(0);
        
        builder.HasIndex(e => e.AccountId).HasDatabaseName("account_id");
    }
}
