using Core.Server;

namespace Char.Server;

/// <summary>
/// Pincode configuration
/// </summary>
public class PincodeConfiguration
{
    /// <summary>
    /// Enable pincode system
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Time in seconds to allow pincode change
    /// </summary>
    public int ChangeTime { get; set; } = 0;
    
    /// <summary>
    /// Maximum pincode attempts
    /// </summary>
    public int MaxTry { get; set; } = 3;
    
    /// <summary>
    /// Force pincode creation
    /// </summary>
    public bool Force { get; set; } = false;
    
    /// <summary>
    /// Allow repeated digits in pincode
    /// </summary>
    public bool AllowRepeated { get; set; } = false;
    
    /// <summary>
    /// Allow sequential digits in pincode
    /// </summary>
    public bool AllowSequential { get; set; } = false;
}

/// <summary>
/// Character move configuration
/// </summary>
public class CharMoveConfiguration
{
    /// <summary>
    /// Enable character move feature
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Move character to used slot
    /// </summary>
    public bool MoveToUsed { get; set; } = false;
    
    /// <summary>
    /// Unlimited character moves
    /// </summary>
    public bool Unlimited { get; set; } = false;
}

/// <summary>
/// Character configuration
/// </summary>
public class CharConfiguration
{
    /// <summary>
    /// Maximum chars per account (default unlimited) [Sirius]
    /// </summary>
    public int CharPerAccount { get; set; } = 9;
    
    /// <summary>
    /// From which level u can delete character [Lupus]
    /// </summary>
    public int CharDeleteLevel { get; set; } = 0;
    
    /// <summary>
    /// Minimum delay before effectly do the deletion
    /// </summary>
    public int CharDeleteDelay { get; set; } = 86400; // seconds (24 hours)
    
    /// <summary>
    /// Allow or not identical name for characters but with a different case by [Yor]
    /// </summary>
    public bool NameIgnoringCase { get; set; } = false;
    
    /// <summary>
    /// Name to use when the requested name cannot be determined
    /// </summary>
    public string UnknownCharName { get; set; } = "Unknown";
    
    /// <summary>
    /// List of letters/symbols allowed (or not) in a character name. by [Yor]
    /// </summary>
    public string CharNameLetters { get; set; } = string.Empty;
    
    /// <summary>
    /// Option to know which letters/symbols are authorised in the name of a character 
    /// (0: all, 1: only those in char_name_letters, 2: all EXCEPT those in char_name_letters) by [Yor]
    /// </summary>
    public int CharNameOption { get; set; } = 0;
    
    /// <summary>
    /// Minimum character name length (default: 4)
    /// </summary>
    public byte CharNameMinLength { get; set; } = 4;
    
    /// <summary>
    /// Character deletion type, email = 1, birthdate = 2 (default)
    /// </summary>
    public int CharDeleteOption { get; set; } = 2;
    
    /// <summary>
    /// Character deletion restriction 
    /// (0: none, 1: if the character is in a party, 2: if the character is in a guild, 
    /// 3: if the character is in a party or a guild)
    /// </summary>
    public int CharDeleteRestriction { get; set; } = 0;
    
    /// <summary>
    /// Character renaming in a party
    /// </summary>
    public bool CharRenameParty { get; set; } = false;
    
    /// <summary>
    /// Character renaming in a guild
    /// </summary>
    public bool CharRenameGuild { get; set; } = false;
}

/// <summary>
/// Character server configuration
/// </summary>
public class CharServerConfiguration : Core.Server.ServerConfiguration
{
    /// <summary>
    /// User ID for authentication
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Server name
    /// </summary>
    public string ServerName { get; set; } = "Ragnarok";
    
    /// <summary>
    /// Wisp server name
    /// </summary>
    public string WispServerName { get; set; } = "Server";
    
    /// <summary>
    /// Login server IP address
    /// </summary>
    public string LoginIp { get; set; } = "127.0.0.1";
    
    /// <summary>
    /// Login server port
    /// </summary>
    public ushort LoginPort { get; set; } = 6001;
    
    /// <summary>
    /// Character server IP address
    /// </summary>
    public string CharIp { get; set; } = "127.0.0.1";
    
    /// <summary>
    /// Bind IP address
    /// </summary>
    public string BindIp { get; set; } = "0.0.0.0";
    
    /// <summary>
    /// Character server port
    /// </summary>
    public ushort CharPort { get; set; } = 5002;
    
    /// <summary>
    /// Character server maintenance mode
    /// </summary>
    public int CharMaintenance { get; set; } = 0;
    
    /// <summary>
    /// Allow new character creation
    /// </summary>
    public bool CharNew { get; set; } = true;
    
    /// <summary>
    /// Display new character option
    /// </summary>
    public int CharNewDisplay { get; set; } = 0;
    
    /// <summary>
    /// Character move configuration
    /// </summary>
    public CharMoveConfiguration CharMove { get; set; } = new();
    
    /// <summary>
    /// Character configuration
    /// </summary>
    public CharConfiguration Char { get; set; } = new();
    
    /// <summary>
    /// Pincode configuration
    /// </summary>
    public PincodeConfiguration Pincode { get; set; } = new();
    
    /// <summary>
    /// Show loading/saving messages
    /// </summary>
    public int SaveLog { get; set; } = 1;
    
    /// <summary>
    /// Loggin char or not [devil]
    /// </summary>
    public int LogChar { get; set; } = 1;
    
    /// <summary>
    /// Loggin inter or not [devil]
    /// </summary>
    public int LogInter { get; set; } = 1;
    
    /// <summary>
    /// Checking sql-table at beginning?
    /// </summary>
    public int CharCheckDb { get; set; } = 0;
    
    /// <summary>
    /// Initial position the player will spawn on the server
    /// </summary>
    public List<StartPoint> StartPoint { get; set; } = new();
    
    /// <summary>
    /// Initial position the Doram player will spawn on the server
    /// </summary>
    public List<StartPoint> StartPointDoram { get; set; } = new();
    
    /// <summary>
    /// Initial items the player with spawn with on the server
    /// </summary>
    public List<StartItem> StartItems { get; set; } = new();
    
    /// <summary>
    /// Initial items the Doram player with spawn with on the server
    /// </summary>
    public List<StartItem> StartItemsDoram { get; set; } = new();
    
    /// <summary>
    /// Starting status points
    /// </summary>
    public uint StartStatusPoints { get; set; } = 0;
    
    /// <summary>
    /// Console enabled
    /// </summary>
    public int Console { get; set; } = 1;
    
    /// <summary>
    /// Maximum concurrent connections
    /// </summary>
    public int MaxConnectUser { get; set; } = -1; // -1: unlimited
    
    /// <summary>
    /// GM allow group
    /// </summary>
    public int GmAllowGroup { get; set; } = 99;
    
    /// <summary>
    /// Autosave interval in seconds
    /// </summary>
    public int AutosaveInterval { get; set; } = 300;
    
    /// <summary>
    /// Starting zeny
    /// </summary>
    public int StartZeny { get; set; } = 0;
    
    /// <summary>
    /// Guild experience rate
    /// </summary>
    public int GuildExpRate { get; set; } = 100;
    
    /// <summary>
    /// Days before removing inactive clan members
    /// </summary>
    public int ClanRemoveInactiveDays { get; set; } = 14;
    
    /// <summary>
    /// Days before returning mail
    /// </summary>
    public int MailReturnDays { get; set; } = 15;
    
    /// <summary>
    /// Days before deleting mail
    /// </summary>
    public int MailDeleteDays { get; set; } = 90;
    
    /// <summary>
    /// Mail retrieve setting
    /// </summary>
    public int MailRetrieve { get; set; } = 1;
    
    /// <summary>
    /// Return empty mail
    /// </summary>
    public int MailReturnEmpty { get; set; } = 0;
    
    /// <summary>
    /// Allowed job flag
    /// </summary>
    public int AllowedJobFlag { get; set; } = -1; // -1: all jobs
    
    /// <summary>
    /// Clear parties on startup
    /// </summary>
    public int ClearParties { get; set; } = 0;
}

/// <summary>
/// Starting point for character spawn
/// </summary>
public class StartPoint
{
    /// <summary>
    /// Map name
    /// </summary>
    public string Map { get; set; } = string.Empty;
    
    /// <summary>
    /// X coordinate
    /// </summary>
    public short X { get; set; }
    
    /// <summary>
    /// Y coordinate
    /// </summary>
    public short Y { get; set; }
}

/// <summary>
/// Starting item for new characters
/// </summary>
public class StartItem
{
    /// <summary>
    /// Item ID
    /// </summary>
    public int ItemId { get; set; }
    
    /// <summary>
    /// Item amount
    /// </summary>
    public int Amount { get; set; }
    
    /// <summary>
    /// Is equipped
    /// </summary>
    public bool IsEquipped { get; set; }
}