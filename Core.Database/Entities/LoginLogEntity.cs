namespace Core.Database.Entities;

public class LoginLogEntity
{
    public DateTime Time { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public sbyte RCode { get; set; }
    public string Log { get; set; } = string.Empty;
}

