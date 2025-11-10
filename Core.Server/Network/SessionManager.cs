using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Core.Server.Packets;

namespace Core.Server.Network;

/// <summary>
/// Manages client sessions and provides packet system integration.
/// </summary>
public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions;
    private readonly ILogger _logger;
    private readonly IPacketFactory _packetFactory;
    private readonly IPacketSizeRegistry _sizeRegistry;
    private readonly int _heartbeatTimeout;
    private readonly CancellationTokenSource _cts;
    private readonly Task _cleanupTask;

    public int SessionCount => _sessions.Count;

    public SessionManager(
        IPacketFactory packetFactory,
        IPacketSizeRegistry sizeRegistry,
        ILogger logger,
        int heartbeatTimeout = 30000)
    {
        _sessions = new ConcurrentDictionary<Guid, ClientSession>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        _sizeRegistry = sizeRegistry ?? throw new ArgumentNullException(nameof(sizeRegistry));
        _heartbeatTimeout = heartbeatTimeout;
        
        _cts = new CancellationTokenSource();
        _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
    }

    public ClientSession CreateSession(Socket socket)
    {
        var session = new ClientSession(socket, _heartbeatTimeout, _packetFactory, _sizeRegistry, _logger);
        
        if (_sessions.TryAdd(session.SessionId, session))
        {
            _logger.LogInformation("Created session {SessionId} from {RemoteEndpoint}", 
                session.SessionId, socket.RemoteEndPoint);
            return session;
        }

        // Shouldn't happen, but handle just in case
        session.Dispose();
        throw new InvalidOperationException("Failed to add session to manager");
    }

    public ClientSession? GetSession(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
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
            _logger.LogInformation("Removing session {SessionId}", sessionId);
            session.Dispose();
        }
    }

    public IEnumerable<ClientSession> GetAllSessions()
    {
        return _sessions.Values;
    }

    public async Task FlushAllSessionsAsync()
    {
        var tasks = _sessions.Values.Select(s => s.FlushPacketsAsync());
        await Task.WhenAll(tasks);
    }

    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, cancellationToken);

                var sessionsToRemove = _sessions.Values
                    .Where(s => !s.IsAlive || s.IsHeartbeatTimedOut())
                    .ToList();

                foreach (var session in sessionsToRemove)
                {
                    if (session.IsHeartbeatTimedOut())
                    {
                        session.Disconnect(DisconnectReason.HeartbeatTimeout);
                    }
                    RemoveSession(session.SessionId);
                }

                if (sessionsToRemove.Count > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} dead sessions", sessionsToRemove.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session cleanup loop");
            }
        }
    }

    public async Task DisconnectAllAsync(DisconnectReason reason)
    {
        var sessions = _sessions.Values.ToList();
        
        foreach (var session in sessions)
        {
            session.Disconnect(reason);
        }

        // Give sessions a moment to flush pending data
        await Task.Delay(100);

        foreach (var session in sessions)
        {
            RemoveSession(session.SessionId);
        }
    }

    public async Task CheckHeartbeatsAsync()
    {
        var timedOutSessions = _sessions.Values
            .Where(s => s.IsHeartbeatTimedOut())
            .ToList();

        foreach (var session in timedOutSessions)
        {
            _logger.LogWarning("Session {SessionId} heartbeat timeout", session.SessionId);
            session.Disconnect(DisconnectReason.HeartbeatTimeout);
            RemoveSession(session.SessionId);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _cleanupTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }

        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }

        _sessions.Clear();
        _cts.Dispose();
    }
}

