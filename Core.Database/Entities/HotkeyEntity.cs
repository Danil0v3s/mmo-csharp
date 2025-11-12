namespace Core.Database.Entities;

public class HotkeyEntity
{
    public int CharId { get; set; }
    public byte Hotkey { get; set; }
    public byte Type { get; set; }
    public uint ItemSkillId { get; set; }
    public byte SkillLvl { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

