namespace Core.Database.Entities;

public class CashLogEntity
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public int CharId { get; set; }
    public string Type { get; set; } = "S";
    public string CashType { get; set; } = "O";
    public int Amount { get; set; }
    public string Map { get; set; } = string.Empty;
}

