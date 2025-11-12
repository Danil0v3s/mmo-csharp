namespace Core.Database.Entities;

public class SaleEntity
{
    public uint NameId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int Amount { get; set; }
}

