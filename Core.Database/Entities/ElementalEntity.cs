namespace Core.Database.Entities;

public class ElementalEntity
{
    public int EleId { get; set; }
    public int CharId { get; set; }
    public uint Class { get; set; }
    public uint Mode { get; set; } = 1;
    public uint Hp { get; set; }
    public uint Sp { get; set; }
    public uint MaxHp { get; set; }
    public uint MaxSp { get; set; }
    public uint Atk1 { get; set; }
    public uint Atk2 { get; set; }
    public uint Matk { get; set; }
    public ushort Aspd { get; set; }
    public ushort Def { get; set; }
    public ushort Mdef { get; set; }
    public ushort Flee { get; set; }
    public ushort Hit { get; set; }
    public long LifeTime { get; set; }
}

