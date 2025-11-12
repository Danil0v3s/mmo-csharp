namespace Core.Database.Entities;

public class MercenaryEntity
{
    public int MerId { get; set; }
    public int CharId { get; set; }
    public uint Class { get; set; }
    public uint Hp { get; set; }
    public uint Sp { get; set; }
    public int KillCounter { get; set; }
    public long LifeTime { get; set; }
}

