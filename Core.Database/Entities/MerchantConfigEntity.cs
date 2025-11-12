namespace Core.Database.Entities;

public class MerchantConfigEntity
{
    public string WorldName { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public byte StoreType { get; set; }
    public string Data { get; set; } = string.Empty;
}

