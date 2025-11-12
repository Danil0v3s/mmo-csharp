namespace Core.Database.Entities;

public class GuildAllianceEntity
{
    public int GuildId { get; set; }
    public int Opposition { get; set; }
    public int AllianceId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public GuildEntity? Guild { get; set; }
}

