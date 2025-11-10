using System.Collections.Concurrent;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Registry that manages packet handlers using the strategy pattern.
/// Maps PacketHeader enums to their concrete handler implementations.
/// </summary>
public class PacketHandlerRegistry
{
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _handlers;
    private readonly ILogger _logger;

    public PacketHandlerRegistry(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlers = new ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>>();
    }

    /// <summary>
    /// Registers a handler for a specific packet type.
    /// </summary>
    /// <typeparam name="TPacket">The packet type to handle</typeparam>
    /// <param name="header">The packet header identifier</param>
    /// <param name="handler">The handler implementation</param>
    public void RegisterHandler<TPacket>(PacketHeader header, IPacketHandler<TPacket> handler) 
        where TPacket : IncomingPacket
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        bool added = _handlers.TryAdd(header, async (session, packet) =>
        {
            if (packet is TPacket typedPacket)
            {
                await handler.HandleAsync(session, typedPacket);
            }
            else
            {
                _logger.LogError("Packet type mismatch for header {Header}. Expected {ExpectedType}, got {ActualType}",
                    header, typeof(TPacket).Name, packet.GetType().Name);
            }
        });

        if (!added)
        {
            _logger.LogWarning("Handler for {Header} was already registered and will be overwritten", header);
            _handlers[header] = async (session, packet) =>
            {
                if (packet is TPacket typedPacket)
                {
                    await handler.HandleAsync(session, typedPacket);
                }
            };
        }
    }

    /// <summary>
    /// Attempts to handle a packet using the registered handlers.
    /// </summary>
    /// <param name="session">The session that sent the packet</param>
    /// <param name="packet">The packet to handle</param>
    /// <returns>True if a handler was found and executed, false otherwise</returns>
    public async Task<bool> TryHandlePacketAsync(ClientSession session, IncomingPacket packet)
    {
        if (_handlers.TryGetValue(packet.Header, out var handler))
        {
            await handler(session, packet);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a handler is registered for the given packet header.
    /// </summary>
    public bool HasHandler(PacketHeader header)
    {
        return _handlers.ContainsKey(header);
    }

    /// <summary>
    /// Gets the count of registered handlers.
    /// </summary>
    public int HandlerCount => _handlers.Count;
}

