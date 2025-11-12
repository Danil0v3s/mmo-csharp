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
    }
}
