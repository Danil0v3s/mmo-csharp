namespace Core.Database.Entities;

public class PetEntity
{
    public int PetId { get; set; }
    public uint Class { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public ushort Level { get; set; }
    public uint EggId { get; set; }
    public uint Equip { get; set; }
    public ushort Intimate { get; set; }
    public ushort Hungry { get; set; }
    public byte RenameFlag { get; set; }
    public uint Incubate { get; set; }
    public short Autofeed { get; set; }
}

