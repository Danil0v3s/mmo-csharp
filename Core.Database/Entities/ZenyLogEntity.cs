namespace Core.Database.Entities;

public class ZenyLogEntity
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public int CharId { get; set; }
    public int SrcId { get; set; }
    public string Type { get; set; } = "S";
    public int Amount { get; set; }
    public string Map { get; set; } = string.Empty;
}

