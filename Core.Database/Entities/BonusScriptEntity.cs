namespace Core.Database.Entities;

public class BonusScriptEntity
{
    public int CharId { get; set; }
    public string Script { get; set; } = string.Empty;
    public long Tick { get; set; }
    public ushort Flag { get; set; }
    public byte Type { get; set; }
    public short Icon { get; set; } = -1;
}

