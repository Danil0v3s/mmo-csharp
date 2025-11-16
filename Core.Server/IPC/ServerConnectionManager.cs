using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Core.Server.IPC;

/// <summary>
/// Manages server-to-server connections via gRPC.
/// Tracks connections by type and provides iteration support.
/// </summary>
public class ServerConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, ServerSession> _allSessions;
    private readonly ConcurrentDictionary<ServerType, ConcurrentDictionary<Guid, ServerSession>> _sessionsByType;
    private readonly ILogger _logger;
    private readonly string _localServerName;
    private readonly CancellationTokenSource _cts;
    private readonly Task _monitorTask;

    public int TotalConnectionCount => _allSessions.Count;

    public ServerConnectionManager(string localServerName, ILogger logger)
    {
        _localServerName = localServerName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _allSessions = new ConcurrentDictionary<Guid, ServerSession>();
        _sessionsByType = new ConcurrentDictionary<ServerType, ConcurrentDictionary<Guid, ServerSession>>();
        
        // Initialize type dictionaries
        foreach (ServerType type in Enum.GetValues<ServerType>())
        {
            _sessionsByType[type] = new ConcurrentDictionary<Guid, ServerSession>();
        }
        
        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorConnectionsAsync(_cts.Token));
    }

    public async Task<ServerSession?> AddConnectionAsync(
        string serverName, 
        ServerType serverType, 
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        var session = new ServerSession(serverName, serverType, endpoint, _logger);
        
        if (!await session.ConnectAsync(cancellationToken))
        {
            session.Dispose();
            return null;
        }

        if (!_allSessions.TryAdd(session.SessionId, session))
        {
            _logger.LogWarning("Failed to add session for {ServerType} server '{ServerName}'", 
                serverType, serverName);
            session.Dispose();
            return null;
        }

        if (!_sessionsByType[serverType].TryAdd(session.SessionId, session))
        {
            _allSessions.TryRemove(session.SessionId, out _);
            _logger.LogWarning("Failed to add session to type index for {ServerType} server '{ServerName}'",
                serverType, serverName);
            session.Dispose();
            return null;
        }

        _logger.LogInformation("{LocalServer} established connection to {ServerType} server '{ServerName}' at {Endpoint}",
            _localServerName, serverType, serverName, endpoint);

        return session;
    }

    public ServerSession? GetSession(Guid sessionId)
    {
        _allSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public ServerSession? GetSessionByName(string serverName)
    {
        return _allSessions.Values.FirstOrDefault(s => 
            s.ServerName.Equals(serverName, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ServerSession> GetSessionsByType(ServerType serverType)
    {
        return _sessionsByType[serverType].Values.Where(s => s.IsConnected);
    }

    public IEnumerable<ServerSession> GetAllConnectedSessions()
    {
        return _allSessions.Values.Where(s => s.IsConnected);
    }

    public int GetConnectionCount(ServerType serverType)
    {
        return _sessionsByType[serverType].Values.Count(s => s.IsConnected);
    }

    public bool HasConnection(ServerType serverType)
    {
        return _sessionsByType[serverType].Values.Any(s => s.IsConnected);
    }

    public async Task RemoveSessionAsync(Guid sessionId)
    {
        if (_allSessions.TryRemove(sessionId, out var session))
        {
            _sessionsByType[session.ServerType].TryRemove(sessionId, out _);
            
            _logger.LogInformation("{LocalServer} removing connection to {ServerType} server '{ServerName}'",
                _localServerName, session.ServerType, session.ServerName);
            
            await session.DisconnectAsync();
            session.Dispose();
        }
    }

    private async Task MonitorConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, cancellationToken);

                var sessionsToRemove = _allSessions.Values
                    .Where(s => !s.IsConnected || s.IsHealthCheckTimedOut())
                    .ToList();

                foreach (var session in sessionsToRemove)
                {
                    _logger.LogWarning("Connection to {ServerType} server '{ServerName}' lost or timed out",
                        session.ServerType, session.ServerName);
                    await RemoveSessionAsync(session.SessionId);
                }

                if (sessionsToRemove.Count > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} dead server connections", sessionsToRemove.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in server connection monitor loop");
            }
        }
    }

    public async Task DisconnectAllAsync()
    {
        var sessions = _allSessions.Values.ToList();
        
        _logger.LogInformation("{LocalServer} disconnecting from all {Count} server(s)",
            _localServerName, sessions.Count);

        foreach (var session in sessions)
        {
            await RemoveSessionAsync(session.SessionId);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _monitorTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }

        foreach (var session in _allSessions.Values)
        {
            session.Dispose();
        }

        _allSessions.Clear();
        
        foreach (var dict in _sessionsByType.Values)
        {
            dict.Clear();
        }

        _cts.Dispose();
    }
}

