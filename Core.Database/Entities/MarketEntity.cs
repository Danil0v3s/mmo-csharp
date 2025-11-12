namespace Core.Database.Entities;

public class MarketEntity
{
    public string Name { get; set; } = string.Empty;
    public uint NameId { get; set; }
    public uint Price { get; set; }
    public int Amount { get; set; }
    public byte Flag { get; set; }
}

