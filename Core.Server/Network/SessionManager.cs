using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Core.Server.Network;

public class SessionManager
{
    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions;
    private readonly int _heartbeatTimeout;
    private readonly ILogger _logger;

    public int ActiveSessionCount => _sessions.Count;

    public SessionManager(int heartbeatTimeout, ILogger logger)
    {
        _sessions = new ConcurrentDictionary<Guid, ClientSession>();
        _heartbeatTimeout = heartbeatTimeout;
        _logger = logger;
    }

    public ClientSession CreateSession(Socket socket)
    {
        var session = new ClientSession(socket, _heartbeatTimeout, _logger);
        _sessions[session.SessionId] = session;
        
        _logger.LogInformation("New session created: {SessionId} (Total: {Count})", 
            session.SessionId, _sessions.Count);
        
        return session;
    }

    public bool TryGetSession(Guid sessionId, out ClientSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public void RemoveSession(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            _logger.LogInformation("Session removed: {SessionId} (Total: {Count})", 
                sessionId, _sessions.Count);
        }
    }

    public async Task CheckHeartbeatsAsync()
    {
        var sessionsToRemove = new List<Guid>();

        foreach (var (sessionId, session) in _sessions)
        {
            if (session.IsHeartbeatTimedOut())
            {
                session.Disconnect(DisconnectReason.HeartbeatTimeout);
                sessionsToRemove.Add(sessionId);
            }
        }

        foreach (var sessionId in sessionsToRemove)
        {
            RemoveSession(sessionId);
        }

        await Task.CompletedTask;
    }

    public IEnumerable<ClientSession> GetAllSessions()
    {
        return _sessions.Values;
    }

    public async Task DisconnectAllAsync(DisconnectReason reason)
    {
        foreach (var session in _sessions.Values)
        {
            session.Disconnect(reason);
        }

        foreach (var sessionId in _sessions.Keys.ToList())
        {
            RemoveSession(sessionId);
        }

        await Task.CompletedTask;
    }
}

