namespace Core.Database.Entities;

public class GuildSkillEntity
{
    public int GuildId { get; set; }
    public ushort Id { get; set; }
    public byte Lv { get; set; }
    
    // Navigation properties
    public GuildEntity? Guild { get; set; }
}

