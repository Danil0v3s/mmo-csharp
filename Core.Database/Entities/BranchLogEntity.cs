namespace Core.Database.Entities;

public class BranchLogEntity
{
    public uint BranchId { get; set; }
    public DateTime BranchDate { get; set; }
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string CharName { get; set; } = string.Empty;
    public string Map { get; set; } = string.Empty;
}

