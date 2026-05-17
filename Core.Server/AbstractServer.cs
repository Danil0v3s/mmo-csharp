using Core.Server.IPC;
using Microsoft.Extensions.Logging;

namespace Core.Server;

public abstract class AbstractServer : IServer
{
    protected readonly ILogger Logger;
    protected readonly ServerConfiguration Configuration;
    protected readonly IpcClient IpcClient;
    protected CancellationTokenSource? ServerCts;
    private Task? _ipcReconcileTask;

    public string ServerName { get; }
    public ServerState State { get; protected set; }
    
    /// <summary>
    /// Access to server connection manager for IPC operations.
    /// Use this to iterate through connected servers by type.
    /// </summary>
    public ServerConnectionManager ServerConnections => IpcClient.ConnectionManager;

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

            // Initial peer dial. Failures aren't fatal — the reconcile
            // loop below will keep retrying.
            await IpcClient.ConnectToServersAsync(ServerCts.Token);

            // Periodic re-dial of any peer that went away (e.g. after
            // a restart). Without this, the bootstrap pass above is
            // one-shot and every peer restart leaves the outbound
            // link permanently dead.
            _ipcReconcileTask = Task.Run(
                () => IpcClient.RunReconcileLoopAsync(ServerCts.Token),
                ServerCts.Token);

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

            if (_ipcReconcileTask != null)
            {
                try { await _ipcReconcileTask; } catch (OperationCanceledException) { }
                _ipcReconcileTask = null;
            }

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

