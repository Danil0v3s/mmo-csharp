namespace Core.Database.Entities;

public class MemoEntity
{
    public int MemoId { get; set; }
    public int CharId { get; set; }
    public string Map { get; set; } = string.Empty;
    public ushort X { get; set; }
    public ushort Y { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

