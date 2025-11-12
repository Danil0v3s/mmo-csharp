namespace Core.Database.Entities;

public class ChatLogEntity
{
    public long Id { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; } = "O";
    public int TypeId { get; set; }
    public int SrcCharId { get; set; }
    public int SrcAccountId { get; set; }
    public string SrcMap { get; set; } = string.Empty;
    public short SrcMapX { get; set; }
    public short SrcMapY { get; set; }
    public string DstCharName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

