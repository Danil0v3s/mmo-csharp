namespace Core.Database.Entities;

public class GuildStorageLogEntity
{
    public int Id { get; set; }
    public int GuildId { get; set; }
    public DateTime Time { get; set; }
    public int CharId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint NameId { get; set; }
    public int Amount { get; set; } = 1;
    public short Identify { get; set; }
    public byte Refine { get; set; }
    public byte Attribute { get; set; }
    public uint Card0 { get; set; }
    public uint Card1 { get; set; }
    public uint Card2 { get; set; }
    public uint Card3 { get; set; }
    public short OptionId0 { get; set; }
    public short OptionVal0 { get; set; }
    public sbyte OptionParm0 { get; set; }
    public short OptionId1 { get; set; }
    public short OptionVal1 { get; set; }
    public sbyte OptionParm1 { get; set; }
    public short OptionId2 { get; set; }
    public short OptionVal2 { get; set; }
    public sbyte OptionParm2 { get; set; }
    public short OptionId3 { get; set; }
    public short OptionVal3 { get; set; }
    public sbyte OptionParm3 { get; set; }
    public short OptionId4 { get; set; }
    public short OptionVal4 { get; set; }
    public sbyte OptionParm4 { get; set; }
    public uint ExpireTime { get; set; }
    public ulong UniqueId { get; set; }
    public byte Bound { get; set; }
    public byte EnchantGrade { get; set; }
}

