using Core.Server;

namespace Login.Server;

/// <summary>
/// Login server specific configuration
/// </summary>
public class LoginServerConfiguration : ServerConfiguration
{
    // IP Ban Settings
    
    /// <summary>
    /// Interval (in seconds) to clean up expired IP bans
    /// </summary>
    public uint IpBanCleanupInterval { get; set; } = 60;
    
    /// <summary>
    /// Interval (in minutes) to execute a DNS/IP update (for dynamic IPs)
    /// </summary>
    public uint IpSyncInterval { get; set; } = 10;
    
    /// <summary>
    /// Perform IP blocking (via contents of `ipbanlist`)?
    /// </summary>
    public bool IpBan { get; set; } = true;
    
    /// <summary>
    /// Automatic IP blocking due to failed login attempts?
    /// </summary>
    public bool DynamicPassFailureBan { get; set; } = true;
    
    /// <summary>
    /// How far to scan the loginlog for password failures in minutes
    /// </summary>
    public uint DynamicPassFailureBanInterval { get; set; } = 5;
    
    /// <summary>
    /// Number of failures needed to trigger the ipban
    /// </summary>
    public uint DynamicPassFailureBanLimit { get; set; } = 7;
    
    /// <summary>
    /// Duration of the ipban in minutes
    /// </summary>
    public uint DynamicPassFailureBanDuration { get; set; } = 5;
    
    /// <summary>
    /// DNS blacklist blocking?
    /// </summary>
    public bool UseDnsbl { get; set; } = false;
    
    /// <summary>
    /// Comma-separated list of dnsbl servers
    /// </summary>
    public string DnsblServers { get; set; } = string.Empty;

    // Logging Settings
    
    /// <summary>
    /// Whether to log login server actions or not
    /// </summary>
    public bool LogLogin { get; set; } = true;
    
    /// <summary>
    /// Date format used in messages
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    
    /// <summary>
    /// Console input system enabled?
    /// </summary>
    public bool Console { get; set; } = true;

    // Account Creation Settings
    
    /// <summary>
    /// Autoregistration via _M/_F?
    /// </summary>
    public bool NewAccountFlag { get; set; } = true;
    
    /// <summary>
    /// Minimum account name length
    /// </summary>
    public byte AccountNameMinLength { get; set; } = 4;
    
    /// <summary>
    /// Minimum password length
    /// </summary>
    public byte PasswordMinLength { get; set; } = 4;
    
    /// <summary>
    /// New account expiration time (-1: unlimited)
    /// </summary>
    public int StartLimitedTime { get; set; } = -1;
    
    /// <summary>
    /// Work with password hashes instead of plaintext passwords?
    /// </summary>
    public bool UseMd5Passwords { get; set; } = false;

    // Group/Permission Settings
    
    /// <summary>
    /// Required group id to connect
    /// </summary>
    public int GroupIdToConnect { get; set; } = -1;
    
    /// <summary>
    /// Minimum group id to connect
    /// </summary>
    public int MinGroupIdToConnect { get; set; } = -1;

    // Registration Rate Limiting
    
    /// <summary>
    /// Max number of registration
    /// </summary>
    public int AllowedRegistrations { get; set; } = 3;
    
    /// <summary>
    /// Registration interval in seconds
    /// </summary>
    public int RegistrationInterval { get; set; } = 3600;

    // Client Hash Verification
    
    /// <summary>
    /// Flags for checking client md5
    /// </summary>
    public int ClientHashCheck { get; set; } = 0;
    
    /// <summary>
    /// Linked list containing md5 hash for each gm group
    /// </summary>
    public List<ClientHashNode> ClientHashNodes { get; set; } = new();

    // User Count Display Settings
    
    /// <summary>
    /// Disable colorization and description in general?
    /// </summary>
    public bool UserCountDisable { get; set; } = false;
    
    /// <summary>
    /// Amount of users that will display in green
    /// </summary>
    public int UserCountLow { get; set; } = 100;
    
    /// <summary>
    /// Amount of users that will display in yellow
    /// </summary>
    public int UserCountMedium { get; set; } = 500;
    
    /// <summary>
    /// Amount of users that will display in red
    /// </summary>
    public int UserCountHigh { get; set; } = 1000;

    // Character Slot Settings
    
    /// <summary>
    /// Number of characters an account can have
    /// </summary>
    public int CharactersPerAccount { get; set; } = 9;
    
    /// <summary>
    /// VIP system configuration
    /// </summary>
    public VipConfiguration? Vip { get; set; } = new();

    // Web Authentication Token
    
    /// <summary>
    /// Enable web authentication token system
    /// </summary>
    public bool UseWebAuthToken { get; set; } = false;
    
    /// <summary>
    /// Delay disabling web token after char logs off in milliseconds
    /// </summary>
    public int DisableWebTokenDelay { get; set; } = 10000;
}

/// <summary>
/// Client hash node for MD5 verification per group
/// </summary>
public class ClientHashNode
{
    /// <summary>
    /// Group ID for this hash node
    /// </summary>
    public int GroupId { get; set; }
    
    /// <summary>
    /// MD5 hash for client verification
    /// </summary>
    public string Md5Hash { get; set; } = string.Empty;
}

/// <summary>
/// VIP system configuration
/// </summary>
public class VipConfiguration
{
    /// <summary>
    /// VIP group ID
    /// </summary>
    public uint GroupId { get; set; } = 0;
    
    /// <summary>
    /// Number of char-slot to increase in VIP state
    /// </summary>
    public uint CharacterSlotIncrease { get; set; } = 0;
}