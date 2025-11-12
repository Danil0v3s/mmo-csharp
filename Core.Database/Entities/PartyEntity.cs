namespace Core.Database.Entities;

public class PartyEntity
{
    public int PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Exp { get; set; }
    public byte Item { get; set; }
    public int LeaderId { get; set; }
    public int LeaderChar { get; set; }
    
    // Navigation properties
    public ICollection<CharEntity> Members { get; set; } = new List<CharEntity>();
}

