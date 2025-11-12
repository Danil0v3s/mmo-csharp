namespace Core.Database.Entities;

public class BuyingStoreEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string Sex { get; set; } = "M";
    public string Map { get; set; } = string.Empty;
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public string Title { get; set; } = string.Empty;
    public uint Limit { get; set; }
    public string BodyDirection { get; set; } = "4";
    public string HeadDirection { get; set; } = "0";
    public string Sit { get; set; } = "1";
    public sbyte Autotrade { get; set; }
    
    // Navigation properties
    public ICollection<BuyingStoreItemEntity> Items { get; set; } = new List<BuyingStoreItemEntity>();
}

