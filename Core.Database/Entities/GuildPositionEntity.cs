namespace Core.Database.Entities;

public class GuildPositionEntity
{
    public int GuildId { get; set; }
    public byte Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort Mode { get; set; }
    public byte ExpMode { get; set; }
    
    // Navigation properties
    public GuildEntity? Guild { get; set; }
}

