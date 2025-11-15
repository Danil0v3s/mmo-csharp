using Core.Server.Packets;

namespace Core.Server.Network;

/// <summary>
/// Base interface for packet handlers.
/// Implementations are discovered via reflection and instantiated via DI.
/// Decorate implementations with [PacketHandler(PacketHeader.XX)] attribute.
/// </summary>
/// <typeparam name="TSession">The specific session type this handler uses</typeparam>
/// <typeparam name="TPacket">The specific packet type this handler processes</typeparam>
public interface IPacketHandler<in TSession, in TPacket> 
    where TSession : ClientSession
    where TPacket : IncomingPacket
{
    /// <summary>
    /// Handles a specific packet type from a client session.
    /// </summary>
    /// <param name="session">The session that sent the packet</param>
    /// <param name="packet">The typed packet to handle</param>
    Task HandleAsync(TSession session, TPacket packet);
}

