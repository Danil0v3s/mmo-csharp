namespace Core.Database.Entities;

public class SkillEntity
{
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public byte Lv { get; set; }
    public byte Flag { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

