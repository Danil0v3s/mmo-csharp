namespace Core.Database.Entities;

public class FeedingLogEntity
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public int CharId { get; set; }
    public int TargetId { get; set; }
    public short TargetClass { get; set; }
    public string Type { get; set; } = "P";
    public uint Intimacy { get; set; }
    public uint ItemId { get; set; }
    public string Map { get; set; } = string.Empty;
    public ushort X { get; set; }
    public ushort Y { get; set; }
}

