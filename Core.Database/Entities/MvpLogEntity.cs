namespace Core.Database.Entities;

public class MvpLogEntity
{
    public uint MvpId { get; set; }
    public DateTime MvpDate { get; set; }
    public int KillCharId { get; set; }
    public short MonsterId { get; set; }
    public uint Prize { get; set; }
    public ulong MvpExp { get; set; }
    public string Map { get; set; } = string.Empty;
}

