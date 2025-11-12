namespace Core.Database.Entities;

public class IpBanListEntity
{
    public string List { get; set; } = string.Empty;
    public DateTime BTime { get; set; }
    public DateTime RTime { get; set; }
    public string Reason { get; set; } = string.Empty;
}

