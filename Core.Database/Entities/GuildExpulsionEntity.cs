namespace Core.Database.Entities;

public class GuildExpulsionEntity
{
    public int GuildId { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public int CharId { get; set; }
}

