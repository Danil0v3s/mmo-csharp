namespace Core.Server.Network;

public static class PacketIds
{
    public const ushort Heartbeat = 0x0001;
    
    // Login Server packets
    public const ushort LoginRequest = 0x0100;
    public const ushort LoginResponse = 0x0101;
    
    // Char Server packets
    public const ushort CharListRequest = 0x0200;
    public const ushort CharListResponse = 0x0201;
    public const ushort CharCreateRequest = 0x0202;
    public const ushort CharCreateResponse = 0x0203;
    public const ushort CharSelectRequest = 0x0204;
    public const ushort CharSelectResponse = 0x0205;
    
    // Map Server packets
    public const ushort EnterMapRequest = 0x0300;
    public const ushort EnterMapResponse = 0x0301;
    public const ushort MoveRequest = 0x0302;
    public const ushort MoveResponse = 0x0303;
    public const ushort ChatMessage = 0x0304;
}

