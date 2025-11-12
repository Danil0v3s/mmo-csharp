using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BonusScriptEntityConfiguration : IEntityTypeConfiguration<BonusScriptEntity>
{
    public void Configure(EntityTypeBuilder<BonusScriptEntity> builder)
    {
        builder.ToTable("bonus_script");
        builder.HasKey(e => e.CharId);
    }
}
