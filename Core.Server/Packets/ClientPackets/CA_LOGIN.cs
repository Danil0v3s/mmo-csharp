namespace Core.Server.Packets.ClientPackets;

/// <summary>
/// Example 2: Variable-length incoming packet with fixed string fields.
/// Client sends this to authenticate with the login server.
/// Packet structure: [Header (2)] [Size (2)] [Username (24)] [Password (24)] [ClientType (1)]
/// Total size: 53 bytes
/// </summary>
[PacketVersion(1)]
public class CA_LOGIN : IncomingPacket
{
    private const int UsernameLength = 24;
    private const int PasswordLength = 24;
    
    public string Username { get; internal set; } = string.Empty; // 24 bytes
    public string Password { get; internal set; } = string.Empty; // 24 bytes
    public byte ClientType { get; internal set; } // 1 byte
    
    public CA_LOGIN() : base(PacketHeader.CA_LOGIN, isFixedLength: false)
    {
    }
    
    public override void Read(BinaryReader reader)
    {
        Username = reader.ReadFixedString(UsernameLength);
        Password = reader.ReadFixedString(PasswordLength);
        ClientType = reader.ReadByte();
    }
    
    public override int GetSize() => 2 + 2 + UsernameLength + PasswordLength + 1; // 53 bytes total
    
    /// <summary>
    /// Static factory method for creating and reading this packet.
    /// </summary>
    public static CA_LOGIN Create(BinaryReader reader)
    {
        var packet = new CA_LOGIN();
        packet.Read(reader);
        return packet;
    }
}

