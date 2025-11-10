namespace Core.Server.Network;

/// <summary>
/// DEPRECATED: Use Core.Server.Packets.PacketHeader instead.
/// This class is kept for backward compatibility but will be removed in future versions.
/// </summary>
[Obsolete("Use Core.Server.Packets.PacketHeader enum instead. This class will be removed in a future version.")]
public static class PacketIds
{
    public const ushort Heartbeat = 0x0360; // Maps to PacketHeader.CZ_HEARTBEAT
    
    // Login Server packets
    public const ushort LoginRequest = 0x0064;   // Maps to PacketHeader.CA_LOGIN
    public const ushort LoginResponse = 0x0069;  // Maps to PacketHeader.AC_ACCEPT_LOGIN
    
    // Char Server packets
    public const ushort CharListRequest = 0x09a1;  // Maps to PacketHeader.CH_CHARLIST_REQ
    public const ushort CharListResponse = 0x006b; // Maps to PacketHeader.HC_CHARACTER_LIST
    public const ushort CharCreateRequest = 0x0067; // Maps to PacketHeader.CH_MAKE_CHAR
    public const ushort CharCreateResponse = 0x006d; // Maps to PacketHeader.HC_ACCEPT_MAKECHAR
    public const ushort CharSelectRequest = 0x0066; // Maps to PacketHeader.CH_SELECT_CHAR
    public const ushort CharSelectResponse = 0x0071; // Maps to PacketHeader.HC_NOTIFY_ZONESVR
    
    // Map Server packets
    public const ushort EnterMapRequest = 0x0072;  // Maps to PacketHeader.CZ_ENTER
    public const ushort EnterMapResponse = 0x0073; // Maps to PacketHeader.ZC_ACCEPT_ENTER
    public const ushort MoveRequest = 0x0085;      // Maps to PacketHeader.CZ_REQUEST_MOVE
    public const ushort MoveResponse = 0x007b;     // Maps to PacketHeader.ZC_NOTIFY_MOVE
    public const ushort ChatMessage = 0x008c;      // Maps to PacketHeader.CZ_REQUEST_CHAT
}

