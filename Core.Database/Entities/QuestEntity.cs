namespace Core.Database.Entities;

public class QuestEntity
{
    public int CharId { get; set; }
    public uint QuestId { get; set; }
    public string State { get; set; } = "0";
    public uint Time { get; set; }
    public uint Count1 { get; set; }
    public uint Count2 { get; set; }
    public uint Count3 { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

