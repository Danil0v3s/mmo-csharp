namespace Core.Database.Entities;

public class GlobalAccRegStrEntity
{
    public int AccountId { get; set; }
    public string Key { get; set; } = string.Empty;
    public uint Index { get; set; }
    public string Value { get; set; } = "0";
}

