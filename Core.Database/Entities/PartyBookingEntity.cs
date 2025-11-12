namespace Core.Database.Entities;

public class PartyBookingEntity
{
    public string WorldName { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string CharName { get; set; } = string.Empty;
    public ushort Purpose { get; set; }
    public byte Assist { get; set; }
    public byte DamageDealer { get; set; }
    public byte Healer { get; set; }
    public byte Tanker { get; set; }
    public ushort MinimumLevel { get; set; }
    public ushort MaximumLevel { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Created { get; set; }
}

