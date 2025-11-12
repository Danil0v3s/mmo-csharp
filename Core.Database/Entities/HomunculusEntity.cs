namespace Core.Database.Entities;

public class HomunculusEntity
{
    public int HomunId { get; set; }
    public int CharId { get; set; }
    public uint Class { get; set; }
    public int PrevClass { get; set; }
    public string Name { get; set; } = string.Empty;
    public short Level { get; set; }
    public ulong Exp { get; set; }
    public int Intimacy { get; set; }
    public short Hunger { get; set; }
    public ushort Str { get; set; }
    public ushort Agi { get; set; }
    public ushort Vit { get; set; }
    public ushort Int { get; set; }
    public ushort Dex { get; set; }
    public ushort Luk { get; set; }
    public uint Hp { get; set; }
    public uint MaxHp { get; set; }
    public uint Sp { get; set; }
    public uint MaxSp { get; set; }
    public ushort SkillPoint { get; set; }
    public short Alive { get; set; } = 1;
    public short RenameFlag { get; set; }
    public short Vaporize { get; set; }
    public short Autofeed { get; set; }
    
    // Navigation properties
    public ICollection<SkillHomunculusEntity> Skills { get; set; } = new List<SkillHomunculusEntity>();
}

