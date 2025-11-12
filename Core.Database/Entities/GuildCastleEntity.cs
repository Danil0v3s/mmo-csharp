namespace Core.Database.Entities;

public class GuildCastleEntity
{
    public int CastleId { get; set; }
    public int GuildId { get; set; }
    public uint Economy { get; set; }
    public uint Defense { get; set; }
    public uint TriggerE { get; set; }
    public uint TriggerD { get; set; }
    public uint NextTime { get; set; }
    public uint PayTime { get; set; }
    public uint CreateTime { get; set; }
    public uint VisibleC { get; set; }
    public uint VisibleG0 { get; set; }
    public uint VisibleG1 { get; set; }
    public uint VisibleG2 { get; set; }
    public uint VisibleG3 { get; set; }
    public uint VisibleG4 { get; set; }
    public uint VisibleG5 { get; set; }
    public uint VisibleG6 { get; set; }
    public uint VisibleG7 { get; set; }
}

