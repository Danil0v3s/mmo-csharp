namespace Core.Database.Entities;

public class VendingItemEntity
{
    public int VendingId { get; set; }
    public ushort Index { get; set; }
    public int CartInventoryId { get; set; }
    public ushort Amount { get; set; }
    public uint Price { get; set; }
    
    // Navigation properties
    public VendingEntity? Vending { get; set; }
}

