using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class LoginEntityConfiguration : IEntityTypeConfiguration<LoginEntity>
{
    public void Configure(EntityTypeBuilder<LoginEntity> builder)
    {
        builder.ToTable("login");
        
        builder.HasKey(e => e.AccountId);
        
        builder.Property(e => e.AccountId)
            .HasColumnName("account_id");
        
        builder.Property(e => e.UserId)
            .HasColumnName("userid")
            .HasMaxLength(23)
            .IsRequired();
        
        builder.Property(e => e.UserPass)
            .HasColumnName("user_pass")
            .HasMaxLength(32)
            .IsRequired();
        
        builder.Property(e => e.Sex)
            .HasColumnName("sex")
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();
        
        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(39)
            .IsRequired();
        
        builder.Property(e => e.GroupId)
            .HasColumnName("group_id");
        
        builder.Property(e => e.State)
            .HasColumnName("state");
        
        builder.Property(e => e.UnbanTime)
            .HasColumnName("unban_time");
        
        builder.Property(e => e.ExpirationTime)
            .HasColumnName("expiration_time");
        
        builder.Property(e => e.LoginCount)
            .HasColumnName("logincount");
        
        builder.Property(e => e.LastLogin)
            .HasColumnName("lastlogin");
        
        builder.Property(e => e.LastIp)
            .HasColumnName("last_ip")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(e => e.Birthdate)
            .HasColumnName("birthdate");
        
        builder.Property(e => e.CharacterSlots)
            .HasColumnName("character_slots");
        
        builder.Property(e => e.Pincode)
            .HasColumnName("pincode")
            .HasMaxLength(4)
            .IsRequired();
        
        builder.Property(e => e.PincodeChange)
            .HasColumnName("pincode_change");
        
        builder.Property(e => e.VipTime)
            .HasColumnName("vip_time");
        
        builder.Property(e => e.OldGroup)
            .HasColumnName("old_group");
        
        builder.Property(e => e.WebAuthToken)
            .HasColumnName("web_auth_token")
            .HasMaxLength(17);
        
        builder.Property(e => e.WebAuthTokenEnabled)
            .HasColumnName("web_auth_token_enabled");
        
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("name");
        
        builder.HasIndex(e => e.WebAuthToken)
            .IsUnique()
            .HasDatabaseName("web_auth_token_key");
        
        // Relationships
        builder.HasMany(e => e.Characters)
            .WithOne(c => c.Account)
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Seed data is applied via DatabaseSeeder from SQL scripts
    }
}

