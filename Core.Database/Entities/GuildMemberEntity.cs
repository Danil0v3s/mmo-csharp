namespace Core.Database.Entities;

public class GuildMemberEntity
{
    public int GuildId { get; set; }
    public int CharId { get; set; }
    public ulong Exp { get; set; }
    public byte Position { get; set; }
    
    // Navigation properties
    public GuildEntity? Guild { get; set; }
}

