using Microsoft.Extensions.Logging;

namespace Core.Server;

public abstract class AbstractServer : IServer
{
    protected readonly ILogger Logger;
    protected readonly ServerConfiguration Configuration;
    protected readonly IpcClient IpcClient;
    protected CancellationTokenSource? ServerCts;

    public string ServerName { get; }
    public ServerState State { get; protected set; }

    protected AbstractServer(string serverName, ServerConfiguration configuration, ILogger logger)
    {
        ServerName = serverName;
        Configuration = configuration;
        Logger = logger;
        IpcClient = new IpcClient(serverName, configuration.OtherServerEndpoints, logger);
        State = ServerState.Stopped;
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (State != ServerState.Stopped)
        {
            Logger.LogWarning("{ServerName} is already running or starting", ServerName);
            return;
        }

        State = ServerState.Starting;
        Logger.LogInformation("{ServerName} starting on port {Port}", ServerName, Configuration.Port);

        try
        {
            ServerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            await OnStartingAsync(ServerCts.Token);
            await IpcClient.ConnectToServersAsync(ServerCts.Token);
            
            State = ServerState.Running;
            Logger.LogInformation("{ServerName} started successfully", ServerName);
        }
        catch (Exception ex)
        {
            State = ServerState.Error;
            Logger.LogError(ex, "{ServerName} failed to start", ServerName);
            throw;
        }
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (State != ServerState.Running)
        {
            Logger.LogWarning("{ServerName} is not running", ServerName);
            return;
        }

        State = ServerState.Stopping;
        Logger.LogInformation("{ServerName} stopping...", ServerName);

        try
        {
            ServerCts?.Cancel();
            await OnStoppingAsync(cancellationToken);
            await IpcClient.DisconnectAsync();
            
            State = ServerState.Stopped;
            Logger.LogInformation("{ServerName} stopped successfully", ServerName);
        }
        catch (Exception ex)
        {
            State = ServerState.Error;
            Logger.LogError(ex, "{ServerName} encountered error during shutdown", ServerName);
            throw;
        }
        finally
        {
            ServerCts?.Dispose();
            ServerCts = null;
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await Task.Delay(1000, cancellationToken);
        await StartAsync(cancellationToken);
    }

    protected abstract Task OnStartingAsync(CancellationToken cancellationToken);
    protected abstract Task OnStoppingAsync(CancellationToken cancellationToken);
}

