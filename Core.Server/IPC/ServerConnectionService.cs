namespace Core.Server.IPC;

/// <summary>
/// Wrapper around ServerConnectionManager for DI registration.
/// The actual manager is set after server initialization.
/// </summary>
public class ServerConnectionService : IServerConnectionService
{
    private ServerConnectionManager? _connectionManager;

    public void SetConnectionManager(ServerConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public IEnumerable<ServerSession> GetSessionsByType(ServerType serverType)
    {
        return _connectionManager?.GetSessionsByType(serverType) ?? Enumerable.Empty<ServerSession>();
    }

    public IEnumerable<ServerSession> GetAllConnectedSessions()
    {
        return _connectionManager?.GetAllConnectedSessions() ?? Enumerable.Empty<ServerSession>();
    }

    public ServerSession? GetSessionByName(string serverName)
    {
        return _connectionManager?.GetSessionByName(serverName);
    }

    public bool HasConnection(ServerType serverType)
    {
        return _connectionManager?.HasConnection(serverType) ?? false;
    }

    public int GetConnectionCount(ServerType serverType)
    {
        return _connectionManager?.GetConnectionCount(serverType) ?? 0;
    }
}
