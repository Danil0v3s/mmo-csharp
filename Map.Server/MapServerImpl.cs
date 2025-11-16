using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;

namespace Map.Server;

public class MapServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;
    private readonly PacketHandlerRegistry _handlerRegistry;

    public MapServerImpl(
        ServerConfiguration configuration,
        ILogger<MapServerImpl> logger,
        IServiceProvider serviceProvider,
        ConcurrentDictionary<long, PlayerEntity> players,
        ConcurrentDictionary<Guid, long> sessionToCharacter)
        : base("MapServer", configuration, logger)
    {
        _players = players;
        _sessionToCharacter = sessionToCharacter;
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("MapServer TCP listener started on port {Port}", Configuration.Port);

        _ = Task.Run(async () => await AcceptClientsAsync(cancellationToken), cancellationToken);

        await Task.CompletedTask;
    }

    protected override async Task StopTcpListenerAsync(CancellationToken cancellationToken)
    {
        if (_listenerSocket != null)
        {
            _listenerSocket.Close();
            _listenerSocket.Dispose();
            _listenerSocket = null;
        }

        Logger.LogInformation("MapServer TCP listener stopped");
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listenerSocket != null)
        {
            try
            {
                var clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);
                var session = SessionManager.CreateSession(clientSocket);
                Logger.LogInformation("Client connected: {SessionId}", session.SessionId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error accepting client");
            }
        }
    }

    protected override async Task ProcessIncomingPacketsAsync(double deltaTime, CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await _handlerRegistry.ProcessSessionPacketsAsync(session, Logger);
        }
    }

    private void BroadcastToMap(uint mapId, byte[] packet, long? excludeCharacterId = null)
    {
        foreach (var player in _players.Values.Where(p => p.MapId == mapId))
        {
            if (excludeCharacterId.HasValue && player.CharacterId == excludeCharacterId.Value)
                continue;

            var session = SessionManager.GetAllSessions()
                .FirstOrDefault(s => s.SessionId == player.SessionId);
            
            session?.EnqueuePacket(packet);
        }
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // Update game logic (AI, physics, etc.)
        // This runs at 60 FPS for MapServer
        
        // Example: Update NPC AI, check collisions, etc.
        // For now, just a placeholder
        
        await Task.CompletedTask;
    }

    protected override async Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await session.FlushPacketsAsync();
        }
    }
}

public class PlayerEntity
{
    public long CharacterId { get; set; }
    public uint MapId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public Guid SessionId { get; set; }
}

