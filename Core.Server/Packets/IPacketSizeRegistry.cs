namespace Core.Server.Packets;

/// <summary>
/// Registry for packet size information.
/// Determines whether packets are fixed or variable-length based on their header.
/// </summary>
public interface IPacketSizeRegistry
{
    /// <summary>
    /// Registers a packet header as having a fixed size.
    /// </summary>
    /// <param name="header">The packet header</param>
    /// <param name="size">The fixed size in bytes (including header)</param>
    void RegisterFixedSize(PacketHeader header, int size);
    
    /// <summary>
    /// Registers a packet header as having a variable size.
    /// </summary>
    /// <param name="header">The packet header</param>
    void RegisterVariableSize(PacketHeader header);
    
    /// <summary>
    /// Checks if a packet is fixed-length.
    /// </summary>
    /// <param name="header">The packet header to check</param>
    /// <returns>True if fixed-length, false if variable-length</returns>
    bool IsFixedLength(PacketHeader header);
    
    /// <summary>
    /// Gets the fixed size for a packet header.
    /// </summary>
    /// <param name="header">The packet header to query</param>
    /// <returns>The fixed size if registered as fixed-length, null if variable-length or not registered</returns>
    int? GetFixedSize(PacketHeader header);
    
    /// <summary>
    /// Initializes the registry by scanning all packet types in the assembly.
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Checks if a packet header is registered.
    /// </summary>
    /// <param name="header">The packet header to check</param>
    /// <returns>True if the header is registered</returns>
    bool IsRegistered(PacketHeader header);
}

