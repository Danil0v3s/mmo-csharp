namespace Core.Database.Entities;

public class NpcLogEntity
{
    public uint NpcId { get; set; }
    public DateTime NpcDate { get; set; }
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string CharName { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
}

