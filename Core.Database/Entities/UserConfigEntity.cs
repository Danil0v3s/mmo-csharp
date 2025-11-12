namespace Core.Database.Entities;

public class UserConfigEntity
{
    public string WorldName { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Data { get; set; } = string.Empty;
}

