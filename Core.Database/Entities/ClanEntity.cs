namespace Core.Database.Entities;

public class ClanEntity
{
    public int ClanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Master { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public ushort MaxMember { get; set; }
    
    // Navigation properties
    public ICollection<ClanAllianceEntity> Alliances { get; set; } = new List<ClanAllianceEntity>();
    public ICollection<CharEntity> Members { get; set; } = new List<CharEntity>();
}

