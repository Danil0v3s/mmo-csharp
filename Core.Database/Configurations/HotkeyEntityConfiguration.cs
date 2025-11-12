using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class HotkeyEntityConfiguration : IEntityTypeConfiguration<HotkeyEntity>
{
    public void Configure(EntityTypeBuilder<HotkeyEntity> builder)
    {
        builder.ToTable("hotkey");
        builder.HasKey(e => new { e.CharId, e.Hotkey });
    }
}
