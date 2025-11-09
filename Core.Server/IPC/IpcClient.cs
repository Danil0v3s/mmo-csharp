using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Core.Server;

public class IpcClient
{
    private readonly string _serverName;
    private readonly Dictionary<string, string> _endpoints;
    private readonly Dictionary<string, GrpcChannel> _channels;
    private readonly ILogger _logger;

    public IpcClient(string serverName, Dictionary<string, string> endpoints, ILogger logger)
    {
        _serverName = serverName;
        _endpoints = endpoints;
        _channels = new Dictionary<string, GrpcChannel>();
        _logger = logger;
    }

    public async Task ConnectToServersAsync(CancellationToken cancellationToken)
    {
        foreach (var (serverName, endpoint) in _endpoints)
        {
            try
            {
                var channel = GrpcChannel.ForAddress(endpoint);
                _channels[serverName] = channel;
                _logger.LogInformation("{ServerName} connected to {TargetServer} at {Endpoint}", 
                    _serverName, serverName, endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to {TargetServer} at {Endpoint}", 
                    serverName, endpoint);
            }
        }

        await Task.CompletedTask;
    }

    public GrpcChannel? GetChannel(string serverName)
    {
        _channels.TryGetValue(serverName, out var channel);
        return channel;
    }

    public async Task DisconnectAsync()
    {
        foreach (var (serverName, channel) in _channels)
        {
            try
            {
                await channel.ShutdownAsync();
                _logger.LogInformation("{ServerName} disconnected from {TargetServer}", 
                    _serverName, serverName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting from {TargetServer}", serverName);
            }
        }

        _channels.Clear();
    }
}

