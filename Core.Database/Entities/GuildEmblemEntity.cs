namespace Core.Database.Entities;

public class GuildEmblemEntity
{
    public string WorldName { get; set; } = string.Empty;
    public int GuildId { get; set; }
    public string FileType { get; set; } = string.Empty;
    public byte[]? FileData { get; set; }
    public uint Version { get; set; }
}

