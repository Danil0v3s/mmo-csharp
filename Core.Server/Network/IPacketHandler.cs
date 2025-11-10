namespace Core.Server.Network;

/// <summary>
/// Interface for individual packet handlers.
/// Each packet type should have its own concrete implementation.
/// </summary>
/// <typeparam name="TPacket">The specific packet type this handler processes</typeparam>
public interface IPacketHandler<in TPacket> where TPacket : Packets.IncomingPacket
{
    /// <summary>
    /// Handles a specific packet type from a client session.
    /// </summary>
    /// <param name="session">The session that sent the packet</param>
    /// <param name="packet">The typed packet to handle</param>
    Task HandleAsync(ClientSession session, TPacket packet);
}

