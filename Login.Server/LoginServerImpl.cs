using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;

namespace Login.Server;

using System.Collections.Concurrent;

public class LoginServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;

    // Character server data - equivalent to C++ ch_server array
    private readonly CharServerData[] _charServers = new CharServerData[5]; // MAX_SERVERS = 5

    // Track character server sessions by account ID
    private readonly ConcurrentDictionary<int, string> _charServerSessions = new();

    public LoginServerImpl(
        ServerConfiguration configuration,
        ILogger<LoginServerImpl> logger,
        IServiceProvider serviceProvider)
        : base("LoginServer", configuration, logger)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();

        // Initialize character server array
        for (int i = 0; i < _charServers.Length; i++)
        {
            _charServers[i] = new CharServerData
            {
                Name = string.Empty,
                SocketFd = -1,
                Ip = 0,
                Port = 0,
                Users = 0,
                Type = 0,
                New = 0
            };
        }
    }

    /// <summary>
    /// Adds a character server to the internal tracking array
    /// </summary>
    public void AddCharServer(int serverId, string serverName, uint serverIp, ushort serverPort, ushort serverType, ushort newServer)
    {
        if (serverId >= 0 && serverId < _charServers.Length)
        {
            _charServers[serverId] = new CharServerData
            {
                Name = serverName,
                SocketFd = -1, // Will be set to the actual socket fd in C++, but we track differently in C#
                Ip = serverIp,
                Port = serverPort,
                Users = 0,
                Type = serverType,
                New = newServer
            };

            // Track the session for this character server
            _charServerSessions[serverId] = serverName;
        }
    }

    /// <summary>
    /// Removes a character server from tracking
    /// </summary>
    public void RemoveCharServer(int serverId)
    {
        if (serverId >= 0 && serverId < _charServers.Length)
        {
            _charServerSessions.TryRemove(serverId, out _);
            _charServers[serverId] = new CharServerData
            {
                Name = string.Empty,
                SocketFd = -1,
                Ip = 0,
                Port = 0,
                Users = 0,
                Type = 0,
                New = 0
            };
        }
    }

    /// <summary>
    /// Gets character server data by ID
    /// </summary>
    public CharServerData? GetCharServer(int serverId)
    {
        if (serverId >= 0 && serverId < _charServers.Length)
        {
            return _charServers[serverId];
        }
        return null;
    }

    /// <summary>
    /// Gets all active character servers
    /// </summary>
    public IEnumerable<CharServerData> GetActiveCharServers()
    {
        return _charServers.Where(cs => !string.IsNullOrEmpty(cs.Name));
    }

    /// <summary>
    /// Updates character server user count
    /// </summary>
    public void UpdateCharServerUserCount(int serverId, ushort userCount)
    {
        if (serverId >= 0 && serverId < _charServers.Length)
        {
            _charServers[serverId] = _charServers[serverId] with { Users = userCount };
        }
    }

    /// <summary>
    /// Checks if a given account ID corresponds to a character server
    /// </summary>
    public bool IsCharacterServer(int accountId)
    {
        return _charServerSessions.ContainsKey(accountId);
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("LoginServer TCP listener started on port {Port}", Configuration.Port);

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

        Logger.LogInformation("LoginServer TCP listener stopped");
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

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // Login server doesn't have much game logic
        // Could implement login rate limiting, session cleanup, etc.
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

