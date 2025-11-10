namespace Core.Server.Packets.ClientPackets;

/// <summary>
/// Example 1: Fixed-length incoming packet (heartbeat).
/// Client sends this periodically to maintain connection.
/// Packet structure: [Header (2 bytes)]
/// Total size: 2 bytes
/// </summary>
[PacketVersion(1)]
public class CZ_HEARTBEAT : IncomingPacket
{
    public CZ_HEARTBEAT() : base(PacketHeader.CZ_HEARTBEAT, isFixedLength: true)
    {
    }
    
    public override void Read(BinaryReader reader)
    {
        // No body, header only
    }
    
    public override int GetSize() => 2; // Just header
    
    /// <summary>
    /// Static factory method for creating and reading this packet.
    /// </summary>
    public static CZ_HEARTBEAT Create(BinaryReader reader)
    {
        var packet = new CZ_HEARTBEAT();
        packet.Read(reader);
        return packet;
    }
}

