using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class VendingEntityConfiguration : IEntityTypeConfiguration<VendingEntity>
{
    public void Configure(EntityTypeBuilder<VendingEntity> builder)
    {
        builder.ToTable("vendings");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.Sex).HasColumnName("sex").HasConversion<string>().HasMaxLength(1).IsRequired().HasDefaultValue("M");
        builder.Property(e => e.Map).HasColumnName("map").HasMaxLength(20).IsRequired();
        builder.Property(e => e.X).HasColumnName("x");
        builder.Property(e => e.Y).HasColumnName("y");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(80).IsRequired();
        builder.Property(e => e.BodyDirection).HasColumnName("body_direction").HasMaxLength(1).IsRequired().HasDefaultValue("4");
        builder.Property(e => e.HeadDirection).HasColumnName("head_direction").HasMaxLength(1).IsRequired().HasDefaultValue("0");
        builder.Property(e => e.Sit).HasColumnName("sit").HasMaxLength(1).IsRequired().HasDefaultValue("1");
        builder.Property(e => e.Autotrade).HasColumnName("autotrade");
    }
}
