namespace Core.Database.Entities;

public class LoginEntity
{
    public int AccountId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserPass { get; set; } = string.Empty;
    public string Sex { get; set; } = "M";
    public string Email { get; set; } = string.Empty;
    public byte GroupId { get; set; }
    public uint State { get; set; }
    public uint UnbanTime { get; set; }
    public uint ExpirationTime { get; set; }
    public uint LoginCount { get; set; }
    public DateTime? LastLogin { get; set; }
    public string LastIp { get; set; } = string.Empty;
    public DateOnly? Birthdate { get; set; }
    public byte CharacterSlots { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public uint PincodeChange { get; set; }
    public uint VipTime { get; set; }
    public byte OldGroup { get; set; }
    public string? WebAuthToken { get; set; }
    public short WebAuthTokenEnabled { get; set; }
    
    // Navigation properties
    public ICollection<CharEntity> Characters { get; set; } = new List<CharEntity>();
}

