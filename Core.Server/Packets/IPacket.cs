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
    /// Gets the total size of this packet in bytes, including the header and size field (if variable).
    /// For fixed-length packets, this is the exact size.
    /// For variable-length packets, this should be -1.
    /// </summary>
    int Size { get; }
}

