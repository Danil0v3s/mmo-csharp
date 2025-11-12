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
        
        builder.Property(e => e.BranchId).HasColumnName("branch_id");
        builder.Property(e => e.BranchDate).HasColumnName("branch_date");
        builder.Property(e => e.AccountId).HasColumnName("account_id").HasDefaultValue(0);
        builder.Property(e => e.CharId).HasColumnName("char_id").HasDefaultValue(0);
        builder.Property(e => e.CharName).HasColumnName("char_name").HasMaxLength(25).IsRequired().HasDefaultValue("");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(11).IsRequired().HasDefaultValue("");
        
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.CharId);
    }
}
