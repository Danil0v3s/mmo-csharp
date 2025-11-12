namespace Core.Database.Entities;

public class ClanAllianceEntity
{
    public int ClanId { get; set; }
    public int Opposition { get; set; }
    public int AllianceId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public ClanEntity? Clan { get; set; }
}

