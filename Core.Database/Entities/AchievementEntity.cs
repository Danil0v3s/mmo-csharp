namespace Core.Database.Entities;

public class AchievementEntity
{
    public int CharId { get; set; }
    public long Id { get; set; }
    public uint Count1 { get; set; }
    public uint Count2 { get; set; }
    public uint Count3 { get; set; }
    public uint Count4 { get; set; }
    public uint Count5 { get; set; }
    public uint Count6 { get; set; }
    public uint Count7 { get; set; }
    public uint Count8 { get; set; }
    public uint Count9 { get; set; }
    public uint Count10 { get; set; }
    public DateTime? Completed { get; set; }
    public DateTime? Rewarded { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

