namespace Core.Database.Entities;

public class GuildStoragePayloadEntity
{
    public int GuildId { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
