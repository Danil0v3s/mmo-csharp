namespace Core.Database.Entities;

public class BarterEntity
{
    public string Name { get; set; } = string.Empty;
    public ushort Index { get; set; }
    public ushort Amount { get; set; }
}

