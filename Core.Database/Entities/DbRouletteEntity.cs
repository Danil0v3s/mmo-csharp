namespace Core.Database.Entities;

public class DbRouletteEntity
{
    public int Index { get; set; }
    public ushort Level { get; set; }
    public uint ItemId { get; set; }
    public ushort Amount { get; set; } = 1;
    public ushort Flag { get; set; } = 1;
}

