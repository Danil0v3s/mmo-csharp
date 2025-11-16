using Core.Server.IPC;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Core.Server;

/// <summary>
/// Legacy IPC client - wraps the new ServerConnectionManager for backward compatibility.
/// Consider migrating to use ServerConnectionManager directly.
/// </summary>
public class IpcClient
{
    private readonly string _serverName;
    private readonly Dictionary<string, string> _endpoints;
    private readonly ILogger _logger;
    private readonly ServerConnectionManager _connectionManager;

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

        await Task.CompletedTask;
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

