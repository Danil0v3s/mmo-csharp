using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class BranchLogEntityConfiguration : IEntityTypeConfiguration<BranchLogEntity>
{
    public void Configure(EntityTypeBuilder<BranchLogEntity> builder)
    {
        builder.ToTable("branchlog");
        builder.HasKey(e => e.BranchId);
    }
}
