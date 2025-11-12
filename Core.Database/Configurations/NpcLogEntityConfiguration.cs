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
    }
}
