using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Extension methods for packet handler processing.
/// </summary>
public static class PacketHandlerExtensions
{
    /// <summary>
    /// Processes all queued packets for a session using the packet handler registry.
    /// </summary>
    public static async Task ProcessSessionPacketsAsync(
        this PacketHandlerRegistry registry, 
        ClientSession session, 
        ILogger logger)
    {
        while (session.IncomingPackets.TryDequeue(out var packet))
        {
            try
            {
                bool handled = await registry.TryHandlePacketAsync(session, packet);
                
                if (!handled)
                {
                    logger.LogError(
                        "No handler registered for packet {PacketType} (Header: 0x{Header:X4}) from session {SessionId}. Disconnecting client.",
                        packet.GetType().Name, (short)packet.Header, session.SessionId);
                    session.Disconnect(DisconnectReason.UnhandledPacket);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Error handling packet {PacketType} from session {SessionId}. Disconnecting client.",
                    packet.GetType().Name, session.SessionId);
                session.Disconnect(DisconnectReason.PacketHandlerError);
            }
        }
    }

    /// <summary>
    /// Processes all queued packets for a mock session (for testing).
    /// </summary>
    internal static async Task ProcessMockSessionPacketsAsync(
        this PacketHandlerRegistry registry,
        object session,
        System.Collections.Concurrent.ConcurrentQueue<Packets.IncomingPacket> packets,
        Action<DisconnectReason> disconnectCallback,
        ILogger logger)
    {
        while (packets.TryDequeue(out var packet))
        {
            try
            {
                bool handled = await registry.TryHandlePacketAsync(session, packet);
                
                if (!handled)
                {
                    logger.LogError(
                        "No handler registered for packet {PacketType} (Header: 0x{Header:X4}). Disconnecting client.",
                        packet.GetType().Name, (short)packet.Header);
                    disconnectCallback(DisconnectReason.UnhandledPacket);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Error handling packet {PacketType}. Disconnecting client.",
                    packet.GetType().Name);
                disconnectCallback(DisconnectReason.PacketHandlerError);
            }
        }
    }
}

