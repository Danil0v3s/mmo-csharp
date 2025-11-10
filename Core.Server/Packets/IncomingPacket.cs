namespace Core.Server.Packets;

/// <summary>
/// Base class for packets received from clients.
/// Incoming packets implement the Read method to deserialize from a BinaryReader.
/// </summary>
public abstract class IncomingPacket : Packet
{
    protected IncomingPacket(PacketHeader header, bool isFixedLength) 
        : base(header, isFixedLength)
    {
    }
    
    /// <summary>
    /// Reads packet data from the provided BinaryReader.
    /// Note: The packet header (and size field if variable-length) have already been consumed.
    /// Only read the packet body data.
    /// </summary>
    /// <param name="reader">BinaryReader positioned at the start of the packet body</param>
    public abstract void Read(BinaryReader reader);
}

