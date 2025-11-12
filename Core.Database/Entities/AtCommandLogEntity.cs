namespace Core.Database.Entities;

public class AtCommandLogEntity
{
    public uint AtCommandId { get; set; }
    public DateTime AtCommandDate { get; set; }
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string CharName { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}

