using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Base class for handling packets from client sessions.
/// Provides type-safe packet processing with automatic casting and error handling.
/// </summary>
public abstract class PacketHandler
{
    protected ILogger Logger { get; }

    protected PacketHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected PacketHandler(ILogger<PacketHandler> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a single packet from a client session.
    /// Override this to handle specific packet types.
    /// </summary>
    /// <param name="session">The session that sent the packet</param>
    /// <param name="packet">The incoming packet</param>
    public abstract Task HandlePacketAsync(ClientSession session, IncomingPacket packet);

    /// <summary>
    /// Processes all queued packets for a session.
    /// </summary>
    public async Task ProcessSessionPacketsAsync(ClientSession session)
    {
        while (session.IncomingPackets.TryDequeue(out var packet))
        {
            try
            {
                await HandlePacketAsync(session, packet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling packet {PacketType} from session {SessionId}",
                    packet.GetType().Name, session.SessionId);
            }
        }
    }

    /// <summary>
    /// Helper method to send a packet to a session.
    /// </summary>
    protected void SendPacket(ClientSession session, OutgoingPacket packet)
    {
        session.EnqueuePacket(packet);
    }

    /// <summary>
    /// Helper method to safely cast and handle a specific packet type.
    /// </summary>
    protected bool TryHandlePacket<T>(IncomingPacket packet, Action<T> handler) where T : IncomingPacket
    {
        if (packet is T typedPacket)
        {
            handler(typedPacket);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Helper method to safely cast and handle a specific packet type asynchronously.
    /// </summary>
    protected async Task<bool> TryHandlePacketAsync<T>(IncomingPacket packet, Func<T, Task> handler) where T : IncomingPacket
    {
        if (packet is T typedPacket)
        {
            await handler(typedPacket);
            return true;
        }
        return false;
    }
}

