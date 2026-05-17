using Core.Server.IPC;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Core.Server;

/// <summary>
/// Per-server bootstrap for outbound gRPC peer connections. Owns the
/// <see cref="ServerConnectionManager"/> and is responsible for keeping
/// the connections to configured peers alive across peer restarts.
///
/// <para>Lifecycle:</para>
/// <list type="bullet">
///   <item><see cref="ConnectToServersAsync"/> — initial dial of every
///     configured peer; run once at startup. Failures are logged but
///     not fatal (the reconcile loop will re-attempt).</item>
///   <item><see cref="RunReconcileLoopAsync"/> — long-running loop that
///     re-dials any configured peer whose session has been removed by
///     <see cref="ServerConnectionManager.MonitorConnectionsAsync"/> (e.g.
///     after the peer was restarted). Without this, every peer restart
///     leaves the outbound link permanently dead, which manifests as
///     char-select falling back to accessible-maps, etc.</item>
/// </list>
/// </summary>
public class IpcClient
{
    private readonly string _serverName;
    private readonly Dictionary<string, string> _endpoints;
    private readonly ILogger _logger;
    private readonly ServerConnectionManager _connectionManager;

    /// <summary>How often the reconcile loop scans for missing peer sessions.</summary>
    public TimeSpan ReconcileInterval { get; set; } = TimeSpan.FromSeconds(5);

    public ServerConnectionManager ConnectionManager => _connectionManager;

    public IpcClient(string serverName, Dictionary<string, string> endpoints, ILogger logger)
    {
        _serverName = serverName;
        _endpoints = endpoints;
        _logger = logger;
        _connectionManager = new ServerConnectionManager(serverName, logger);
    }

    public async Task ConnectToServersAsync(CancellationToken cancellationToken)
    {
        foreach (var (serverName, endpoint) in _endpoints)
        {
            var serverType = ParseServerType(serverName);

            try
            {
                var session = await _connectionManager.AddConnectionAsync(
                    serverName, serverType, endpoint, cancellationToken);

                if (session == null)
                {
                    _logger.LogWarning("{ServerName} failed to establish connection to {TargetServer} at {Endpoint} - server may not be running",
                        _serverName, serverName, endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{ServerName} error connecting to {TargetServer} at {Endpoint}",
                    _serverName, serverName, endpoint);
            }
        }
    }

    /// <summary>
    /// Periodically re-dials configured peers that don't have a healthy
    /// session. Pairs with <see cref="ServerConnectionManager.MonitorConnectionsAsync"/>,
    /// which evicts dead sessions but doesn't re-establish them — this
    /// loop is the re-establish side.
    ///
    /// Exits cleanly when <paramref name="cancellationToken"/> is cancelled
    /// (e.g. during server shutdown). Exceptions inside an attempt are
    /// logged and swallowed so one bad endpoint doesn't kill the whole loop.
    /// </summary>
    public async Task RunReconcileLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ReconcileInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ReconcileOnceAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Single reconcile pass. Public so tests can drive it deterministically
    /// without depending on the timer cadence in <see cref="RunReconcileLoopAsync"/>.
    /// </summary>
    public async Task ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var (peerName, endpoint) in _endpoints)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var existing = _connectionManager.GetSessionByName(peerName);
            if (existing != null && existing.IsConnected) continue;

            // Either no session for this peer (initial dial failed) or
            // the previous session was removed by the connection monitor
            // after the peer went away. Drop any zombie and re-dial.
            if (existing != null)
            {
                await _connectionManager.RemoveSessionAsync(existing.SessionId);
            }

            try
            {
                var session = await _connectionManager.AddConnectionAsync(
                    peerName, ParseServerType(peerName), endpoint, cancellationToken);

                if (session != null)
                {
                    _logger.LogInformation(
                        "{ServerName} reconciled connection to {TargetServer} at {Endpoint}",
                        _serverName, peerName, endpoint);
                }
                // Failed redial: AddConnectionAsync already logged at warning.
                // Don't log again — the loop will retry on the next tick.
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "{ServerName} reconcile attempt to {TargetServer} at {Endpoint} threw",
                    _serverName, peerName, endpoint);
            }
        }
    }

    public GrpcChannel? GetChannel(string serverName)
    {
        var session = _connectionManager.GetSessionByName(serverName);
        return session?.IsConnected == true ? session.Channel : null;
    }

    public async Task DisconnectAsync()
    {
        await _connectionManager.DisconnectAllAsync();
    }

    private static ServerType ParseServerType(string serverName)
    {
        if (serverName.Contains("Login", StringComparison.OrdinalIgnoreCase))
            return ServerType.Login;
        if (serverName.Contains("Char", StringComparison.OrdinalIgnoreCase))
            return ServerType.Char;
        if (serverName.Contains("Map", StringComparison.OrdinalIgnoreCase))
            return ServerType.Map;
        if (serverName.Contains("Web", StringComparison.OrdinalIgnoreCase))
            return ServerType.Web;
        
        return ServerType.Login; // Default fallback
    }
}

