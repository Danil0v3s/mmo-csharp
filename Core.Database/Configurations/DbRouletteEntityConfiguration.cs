using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class DbRouletteEntityConfiguration : IEntityTypeConfiguration<DbRouletteEntity>
{
    public void Configure(EntityTypeBuilder<DbRouletteEntity> builder)
    {
        builder.ToTable("db_roulette");
        builder.HasKey(e => e.Index);
        
        builder.Property(e => e.Index).HasColumnName("index").HasDefaultValue(0);
        builder.Property(e => e.Level).HasColumnName("level");
        builder.Property(e => e.ItemId).HasColumnName("item_id");
        builder.Property(e => e.Amount).HasColumnName("amount").HasDefaultValue((ushort)1);
        builder.Property(e => e.Flag).HasColumnName("flag").HasDefaultValue((ushort)1);
    }
}
