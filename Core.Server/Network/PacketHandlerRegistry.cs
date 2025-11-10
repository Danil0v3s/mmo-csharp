using System.Collections.Concurrent;
using System.Reflection;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Registry that discovers and manages packet handlers using reflection.
/// Automatically scans assemblies for classes decorated with [PacketHandler] attribute.
/// Handlers are resolved from the DI container on each invocation.
/// </summary>
public class PacketHandlerRegistry
{
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _handlers;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public PacketHandlerRegistry(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlers = new ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>>();
    }

    /// <summary>
    /// Scans the specified assemblies for packet handlers and registers them.
    /// Handlers are instantiated via DI on each invocation.
    /// </summary>
    public void DiscoverAndRegister(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            DiscoverHandlersInAssembly(assembly);
        }
        
        _logger.LogInformation("Packet handler discovery complete. Discovered {Count} handler(s)", _handlers.Count);
    }

    /// <summary>
    /// Scans the calling assembly for packet handlers and registers them.
    /// </summary>
    public void DiscoverAndRegisterFromCallingAssembly()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        DiscoverHandlersInAssembly(callingAssembly);
        _logger.LogInformation("Packet handler discovery complete. Discovered {Count} handler(s)", _handlers.Count);
    }

    private void DiscoverHandlersInAssembly(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>)));

        foreach (var handlerType in handlerTypes)
        {
            RegisterHandlerType(handlerType);
        }
    }

    private void RegisterHandlerType(Type handlerType)
    {
        var attribute = handlerType.GetCustomAttribute<PacketHandlerAttribute>();
        if (attribute == null) return;

        // Find the IPacketHandler<TPacket> interface
        var handlerInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IPacketHandler<>));
        
        if (handlerInterface == null)
        {
            _logger.LogWarning("Handler type {Type} has [PacketHandler] attribute but doesn't implement IPacketHandler<T>", 
                handlerType.Name);
            return;
        }

        // Get the packet type (TPacket)
        var packetType = handlerInterface.GetGenericArguments()[0];
        var handleMethod = handlerInterface.GetMethod("HandleAsync");

        if (handleMethod == null)
        {
            _logger.LogWarning("HandleAsync method not found on handler type {Type}", handlerType.Name);
            return;
        }

        // Create wrapper that resolves handler from DI container on each invocation
        Func<ClientSession, IncomingPacket, Task> wrapper = async (session, packet) =>
        {
            // Resolve handler instance from DI container
            var handler = _serviceProvider.GetService(handlerType);
            
            if (handler == null)
            {
                _logger.LogError("Failed to resolve handler {Type} from DI container. Ensure it's registered in services.", 
                    handlerType.Name);
                return;
            }

            if (packet.GetType() == packetType || packetType.IsAssignableFrom(packet.GetType()))
            {
                var task = (Task?)handleMethod.Invoke(handler, new object[] { session, packet });
                if (task != null)
                    await task;
            }
            else
            {
                _logger.LogError("Packet type mismatch for header {Header}. Expected {ExpectedType}, got {ActualType}",
                    attribute.Header, packetType.Name, packet.GetType().Name);
            }
        };

        bool added = _handlers.TryAdd(attribute.Header, wrapper);
        if (added)
        {
            _logger.LogDebug("Registered handler {HandlerType} for packet {Header} ({PacketType})", 
                handlerType.Name, attribute.Header, packetType.Name);
        }
        else
        {
            _logger.LogWarning("Duplicate handler registration attempted for {Header}. Handler {HandlerType} will be ignored.", 
                attribute.Header, handlerType.Name);
        }
    }

    /// <summary>
    /// Attempts to handle a packet using registered handlers.
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
    public bool HasHandler(PacketHeader header) => _handlers.ContainsKey(header);
    
    /// <summary>
    /// Gets the count of registered handlers.
    /// </summary>
    public int HandlerCount => _handlers.Count;
}

