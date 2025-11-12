namespace Core.Database.Entities;

public class CharLogEntity
{
    public long Id { get; set; }
    public DateTime Time { get; set; }
    public string CharMsg { get; set; } = "char select";
    public int AccountId { get; set; }
    public byte CharNum { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Str { get; set; }
    public uint Agi { get; set; }
    public uint Vit { get; set; }
    public uint Int { get; set; }
    public uint Dex { get; set; }
    public uint Luk { get; set; }
    public sbyte Hair { get; set; }
    public int HairColor { get; set; }
}

