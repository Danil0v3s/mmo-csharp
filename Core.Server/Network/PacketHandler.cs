using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Base class for handling packets from client sessions using the strategy pattern.
/// Uses a registry to map packet headers to their concrete handler implementations.
/// </summary>
public abstract class PacketHandler
{
    protected ILogger Logger { get; }
    protected PacketHandlerRegistry Registry { get; }

    protected PacketHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Registry = new PacketHandlerRegistry(logger);
        RegisterHandlers();
    }
    
    protected PacketHandler(ILogger<PacketHandler> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Registry = new PacketHandlerRegistry(logger);
        RegisterHandlers();
    }

    /// <summary>
    /// Override this method to register packet handlers in derived classes.
    /// </summary>
    protected abstract void RegisterHandlers();

    /// <summary>
    /// Processes a single packet from a client session using the registered handlers.
    /// </summary>
    /// <param name="session">The session that sent the packet</param>
    /// <param name="packet">The incoming packet</param>
    public async Task HandlePacketAsync(ClientSession session, IncomingPacket packet)
    {
        bool handled = await Registry.TryHandlePacketAsync(session, packet);
        
        if (!handled)
        {
            Logger.LogError("No handler registered for packet {PacketType} (Header: 0x{Header:X4}) from session {SessionId}. Disconnecting client.",
                packet.GetType().Name, (short)packet.Header, session.SessionId);
            session.Disconnect(DisconnectReason.UnhandledPacket);
        }
    }

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
                Logger.LogError(ex, "Error handling packet {PacketType} from session {SessionId}. Disconnecting client.",
                    packet.GetType().Name, session.SessionId);
                session.Disconnect(DisconnectReason.PacketHandlerError);
            }
        }
    }

    /// <summary>
    /// Helper method to send a packet to a session.
    /// </summary>
    protected static void SendPacket(ClientSession session, OutgoingPacket packet)
    {
        session.EnqueuePacket(packet);
    }

    /// <summary>
    /// Helper method to register a handler for a specific packet type.
    /// </summary>
    protected void RegisterHandler<TPacket>(PacketHeader header, IPacketHandler<TPacket> handler) 
        where TPacket : IncomingPacket
    {
        Registry.RegisterHandler(header, handler);
    }
}

