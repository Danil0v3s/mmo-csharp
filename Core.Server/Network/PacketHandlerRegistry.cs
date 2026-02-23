using System.Collections.Concurrent;
using System.Reflection;
using Core.Server.Packets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

/// <summary>
/// Registry that discovers and manages packet handlers using reflection.
/// Automatically scans assemblies for classes decorated with [PacketHandler] attribute.
/// Handlers are resolved from the DI container on each invocation.
/// </summary>
public class PacketHandlerRegistry(IServiceProvider serviceProvider, ILogger logger)
{
    private static readonly TimeSpan HandlerExecutionTimeout = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _handlers = new();
    private readonly ConcurrentDictionary<PacketHeader, Type> _handlerTypes = new();
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<,>)));

        foreach (var handlerType in handlerTypes)
        {
            RegisterHandlerType(handlerType);
        }
    }

    /// <summary>
    /// Manually register a handler instance (useful for handlers with dependencies or testing).
    /// </summary>
    public void RegisterHandler<TSession, TPacket>(PacketHeader header, IPacketHandler<TSession, TPacket> handler)
        where TPacket : IncomingPacket
        where TSession : ClientSession
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        Func<ClientSession, IncomingPacket, Task> wrapper = async (session, packet) =>
        {
            if (session is not TSession typedSession)
            {
                _logger.LogError("Session type mismatch for header {Header}. Expected {ExpectedType}, got {ActualType}",
                    header, typeof(TSession).Name, session.GetType().Name);
                return;
            }
            if (packet is TPacket typedPacket)
            {
                await handler.HandleAsync(typedSession, typedPacket);
            }
            else
            {
                _logger.LogError("Packet type mismatch for header {Header}. Expected {ExpectedType}, got {ActualType}",
                    header, typeof(TPacket).Name, packet.GetType().Name);
            }
        };

        _handlers[header] = wrapper;
        _logger.LogDebug("Manually registered handler for packet {Header}", header);
    }

    private void RegisterHandlerType(Type handlerType)
    {
        var attribute = handlerType.GetCustomAttribute<PacketHandlerAttribute>();
        if (attribute == null) return;

        // Find the IPacketHandler<TPacket> interface
        var handlerInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IPacketHandler<,>));

        if (handlerInterface == null)
        {
            _logger.LogWarning("Handler type {Type} has [PacketHandler] attribute but doesn't implement IPacketHandler<T>",
                handlerType.Name);
            return;
        }

        // IPacketHandler<TSession, TPacket>
        var genericArguments = handlerInterface.GetGenericArguments();
        var sessionType = genericArguments[0];
        var packetType = genericArguments[1];
        var handleMethod = handlerInterface.GetMethod("HandleAsync");

        if (handleMethod == null)
        {
            _logger.LogWarning("HandleAsync method not found on handler type {Type}", handlerType.Name);
            return;
        }

        // Create wrapper that resolves handler from DI container on each invocation
        Func<ClientSession, IncomingPacket, Task> wrapper = async (session, packet) =>
        {
            using var scope = _serviceProvider.CreateScope();
            object handler;
            try
            {
                var resolveTask = Task.Run(() => scope.ServiceProvider.GetRequiredService(handlerType));
                var resolved = await Task.WhenAny(resolveTask, Task.Delay(HandlerExecutionTimeout));
                if (resolved != resolveTask)
                {
                    throw new TimeoutException(
                        $"Timed out resolving handler {handlerType.Name} after {(int)HandlerExecutionTimeout.TotalMilliseconds}ms.");
                }

                handler = await resolveTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resolve handler {HandlerType} for header {Header} (session {SessionId}).",
                    handlerType.Name,
                    attribute.Header,
                    session.SessionId);
                throw;
            }

            if ((session.GetType() == sessionType || sessionType.IsAssignableFrom(session.GetType())) &&
                (packet.GetType() == packetType || packetType.IsAssignableFrom(packet.GetType())))
            {
                try
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { session, packet });
                    if (task != null)
                    {
                        var completed = await Task.WhenAny(task, Task.Delay(HandlerExecutionTimeout));
                        if (completed != task)
                        {
                            throw new TimeoutException(
                                $"Handler {handlerType.Name} timed out after {(int)HandlerExecutionTimeout.TotalMilliseconds}ms for header {attribute.Header}.");
                        }

                        await task;
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    _logger.LogError(
                        ex.InnerException,
                        "Handler {HandlerType} threw for header {Header} (session {SessionId}).",
                        handlerType.Name,
                        attribute.Header,
                        session.SessionId);
                    throw ex.InnerException;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed invoking handler {HandlerType} for header {Header} (session {SessionId}).",
                        handlerType.Name,
                        attribute.Header,
                        session.SessionId);
                    throw;
                }
            }
            else
            {
                _logger.LogError(
                    "Handler type mismatch for header {Header}. Expected session={ExpectedSession} packet={ExpectedPacket}, got session={ActualSession} packet={ActualPacket}",
                    attribute.Header,
                    sessionType.Name,
                    packetType.Name,
                    session.GetType().Name,
                    packet.GetType().Name);
            }
        };

        bool added = _handlers.TryAdd(attribute.Header, wrapper);
        if (added)
        {
            _handlerTypes[attribute.Header] = handlerType;
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
    /// Attempts to handle a packet using registered handlers (for testing with mock session).
    /// For mock sessions, just checks if handler exists without invoking.
    /// </summary>
    internal Task<bool> TryHandlePacketAsync(object session, IncomingPacket packet)
    {
        // For mock sessions, we just check if handler exists
        bool hasHandler = _handlers.ContainsKey(packet.Header);
        return Task.FromResult(hasHandler);
    }

    /// <summary>
    /// Checks if a handler is registered for the given packet header.
    /// </summary>
    public bool HasHandler(PacketHeader header) => _handlers.ContainsKey(header);

    /// <summary>
    /// Gets the count of registered handlers.
    /// </summary>
    public int HandlerCount => _handlers.Count;

    /// <summary>
    /// Resolves every discovered handler once at startup.
    /// This forces DI errors/timeouts to happen during boot instead of first packet dispatch.
    /// </summary>
    public void WarmUpHandlers(TimeSpan timeoutPerHandler, bool failOnError = false)
    {
        using var scope = _serviceProvider.CreateScope();
        foreach (var kvp in _handlerTypes.OrderBy(k => k.Key))
        {
            var header = kvp.Key;
            var handlerType = kvp.Value;
            try
            {
                var resolveTask = Task.Run(() => scope.ServiceProvider.GetRequiredService(handlerType));
                var completed = Task.WhenAny(resolveTask, Task.Delay(timeoutPerHandler)).GetAwaiter().GetResult();
                if (completed != resolveTask)
                {
                    throw new TimeoutException(
                        $"Timed out warming handler {handlerType.Name} for header {header} after {(int)timeoutPerHandler.TotalMilliseconds}ms.");
                }

                _ = resolveTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Warm-up failed resolving handler {HandlerType} for header {Header}",
                    handlerType.Name,
                    header);
                if (failOnError)
                {
                    throw;
                }
            }
        }
    }
}
