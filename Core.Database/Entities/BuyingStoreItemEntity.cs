namespace Core.Database.Entities;

public class BuyingStoreItemEntity
{
    public int BuyingStoreId { get; set; }
    public ushort Index { get; set; }
    public uint ItemId { get; set; }
    public ushort Amount { get; set; }
    public uint Price { get; set; }
    
    // Navigation properties
    public BuyingStoreEntity? BuyingStore { get; set; }
}

