namespace Core.Database.Entities;

public class InterLogEntity
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public string Log { get; set; } = string.Empty;
}

