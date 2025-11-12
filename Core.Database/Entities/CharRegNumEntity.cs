namespace Core.Database.Entities;

public class CharRegNumEntity
{
    public int CharId { get; set; }
    public string Key { get; set; } = string.Empty;
    public uint Index { get; set; }
    public long Value { get; set; }
}

