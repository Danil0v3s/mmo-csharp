namespace Core.Database.Entities;

public class CharConfigEntity
{
    public string WorldName { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string Data { get; set; } = string.Empty;
}

