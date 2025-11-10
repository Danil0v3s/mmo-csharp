namespace Core.Server.Packets;

/// <summary>
/// Base class for packets sent to clients.
/// Outgoing packets implement the Write method to serialize to a BinaryWriter.
/// </summary>
public abstract class OutgoingPacket : Packet
{
    protected OutgoingPacket(PacketHeader header, bool isFixedLength) 
        : base(header, isFixedLength)
    {
    }
    
    /// <summary>
    /// Writes the complete packet to the provided BinaryWriter.
    /// Must write the header, size field (if variable-length), and all packet body data.
    /// </summary>
    /// <param name="writer">BinaryWriter to write the packet to</param>
    public abstract void Write(BinaryWriter writer);
}

