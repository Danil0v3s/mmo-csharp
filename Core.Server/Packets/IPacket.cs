namespace Core.Server.Packets;

/// <summary>
/// Base interface for all packet types in the system.
/// </summary>
public interface IPacket
{
    /// <summary>
    /// The packet header that identifies this packet type.
    /// </summary>
    PacketHeader Header { get; }
    
    /// <summary>
    /// Indicates whether this packet has a fixed length (true) or variable length (false).
    /// Fixed-length packets don't include a size field.
    /// Variable-length packets include a 2-byte size field after the header.
    /// </summary>
    bool IsFixedLength { get; }
    
    /// <summary>
    /// Gets the total size of this packet in bytes, including the header and size field (if variable).
    /// </summary>
    /// <returns>Total packet size in bytes</returns>
    int GetSize();
}

