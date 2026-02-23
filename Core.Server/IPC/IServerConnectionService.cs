namespace Core.Server.IPC;

/// <summary>
/// DI-friendly abstraction over ServerConnectionManager.
/// Allows services to access server connections without depending on the server implementation.
/// </summary>
public interface IServerConnectionService
{
    IEnumerable<ServerSession> GetSessionsByType(ServerType serverType);
    IEnumerable<ServerSession> GetAllConnectedSessions();
    ServerSession? GetSessionByName(string serverName);
    bool HasConnection(ServerType serverType);
    int GetConnectionCount(ServerType serverType);
}
