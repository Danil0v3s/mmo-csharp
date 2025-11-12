namespace Core.Database.Entities;

public class CharRegStrEntity
{
    public int CharId { get; set; }
    public string Key { get; set; } = string.Empty;
    public uint Index { get; set; }
    public string Value { get; set; } = "0";
}

