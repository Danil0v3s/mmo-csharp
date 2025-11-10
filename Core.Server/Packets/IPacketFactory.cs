namespace Core.Server.Packets;

/// <summary>
/// Factory for creating packet instances from incoming data.
/// Supports versioning to allow multiple implementations of the same packet type.
/// </summary>
public interface IPacketFactory
{
    /// <summary>
    /// Registers a packet type for a specific header and version.
    /// </summary>
    /// <typeparam name="T">The packet type to register</typeparam>
    /// <param name="header">The packet header</param>
    /// <param name="version">The version number</param>
    void RegisterPacket<T>(PacketHeader header, int version) where T : IncomingPacket, new();
    
    /// <summary>
    /// Sets the active version for a packet header.
    /// </summary>
    /// <param name="header">The packet header</param>
    /// <param name="version">The version to activate</param>
    void SetActiveVersion(PacketHeader header, int version);
    
    /// <summary>
    /// Creates a packet instance from a BinaryReader.
    /// The packet header must already be read from the reader.
    /// </summary>
    /// <param name="header">The packet header that was read</param>
    /// <param name="reader">The BinaryReader positioned after the header (and size if variable)</param>
    /// <returns>The created packet instance</returns>
    IncomingPacket CreatePacket(PacketHeader header, BinaryReader reader);
    
    /// <summary>
    /// Checks if a packet can be created for the given header.
    /// </summary>
    /// <param name="header">The packet header to check</param>
    /// <returns>True if a packet factory is registered for this header</returns>
    bool CanCreatePacket(PacketHeader header);
    
    /// <summary>
    /// Initializes the factory by scanning all packet types in the assembly.
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Gets the active version for a packet header.
    /// </summary>
    /// <param name="header">The packet header</param>
    /// <returns>The active version number, or null if not set</returns>
    int? GetActiveVersion(PacketHeader header);
}

