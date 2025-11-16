using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Core.Server.IPC;

public class ServerSession : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _endpoint;
    private readonly CancellationTokenSource _healthCheckCts;
    private readonly Task _healthCheckTask;
    
    public Guid SessionId { get; }
    public string ServerName { get; }
    public ServerType ServerType { get; }
    public GrpcChannel Channel { get; private set; }
    public bool IsConnected { get; private set; }
    public long LastHealthCheckTime { get; private set; }
    public int HealthCheckInterval { get; } = 5000; // ms

    public ServerSession(string serverName, ServerType serverType, string endpoint, ILogger logger)
    {
        SessionId = Guid.NewGuid();
        ServerName = serverName;
        ServerType = serverType;
        _endpoint = endpoint;
        _logger = logger;
        
        Channel = GrpcChannel.ForAddress(endpoint);
        IsConnected = false;
        LastHealthCheckTime = Stopwatch.GetTimestamp();
        
        _healthCheckCts = new CancellationTokenSource();
        _healthCheckTask = Task.Run(() => HealthCheckLoopAsync(_healthCheckCts.Token));
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to verify the channel is actually reachable
            await Channel.ConnectAsync(cancellationToken);
            IsConnected = true;
            LastHealthCheckTime = Stopwatch.GetTimestamp();
            
            _logger.LogInformation("Connected to {ServerType} server '{ServerName}' at {Endpoint} (SessionId: {SessionId})",
                ServerType, ServerName, _endpoint, SessionId);
            
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            _logger.LogWarning(ex, "Failed to connect to {ServerType} server '{ServerName}' at {Endpoint}",
                ServerType, ServerName, _endpoint);
            return false;
        }
    }

    private async Task HealthCheckLoopAsync(CancellationToken cancellationToken)
    {
        // Wait a bit before starting health checks
        await Task.Delay(HealthCheckInterval, cancellationToken);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(HealthCheckInterval, cancellationToken);
                
                if (!IsConnected)
                    continue;

                // Perform basic connectivity check
                var state = Channel.State;
                
                if (state == ConnectivityState.TransientFailure || state == ConnectivityState.Shutdown)
                {
                    _logger.LogWarning("{ServerType} server '{ServerName}' is in state {State}, marking as disconnected",
                        ServerType, ServerName, state);
                    IsConnected = false;
                }
                else
                {
                    LastHealthCheckTime = Stopwatch.GetTimestamp();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check loop for {ServerType} server '{ServerName}'",
                    ServerType, ServerName);
            }
        }
    }

    public bool IsHealthCheckTimedOut()
    {
        if (!IsConnected)
            return true;

        var elapsed = (Stopwatch.GetTimestamp() - LastHealthCheckTime) * 1000.0 / Stopwatch.Frequency;
        return elapsed > HealthCheckInterval * 3; // Allow 3 missed checks
    }

    public async Task DisconnectAsync()
    {
        if (!IsConnected)
            return;

        IsConnected = false;
        
        try
        {
            await Channel.ShutdownAsync();
            _logger.LogInformation("Disconnected from {ServerType} server '{ServerName}'", ServerType, ServerName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during graceful disconnect from {ServerType} server '{ServerName}'",
                ServerType, ServerName);
        }
    }

    public void Dispose()
    {
        _healthCheckCts.Cancel();
        
        try
        {
            _healthCheckTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        
        IsConnected = false;
        Channel.Dispose();
        _healthCheckCts.Dispose();
    }
}

