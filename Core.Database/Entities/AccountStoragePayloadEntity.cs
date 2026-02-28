namespace Core.Database.Entities;

public class AccountStoragePayloadEntity
{
    public int AccountId { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
