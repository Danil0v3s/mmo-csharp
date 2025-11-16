using System.Diagnostics;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Core.Server;

public abstract class GameLoopServer : AbstractServer
{
    private readonly SessionManager _sessionManager;
    private readonly PacketSystem _packetSystem;
    private Task? _gameLoopTask;

    protected int TargetFPS => Configuration.TargetFPS;
    protected double TargetFrameTime => 1000.0 / TargetFPS;
    protected SessionManager SessionManager => _sessionManager;
    protected PacketSystem PacketSystem => _packetSystem;

    protected GameLoopServer(string serverName, ServerConfiguration configuration, ILogger logger)
        : base(serverName, configuration, logger)
    {
        // Initialize packet system
        _packetSystem = new PacketSystem();
        _packetSystem.Initialize();
        
        // Create session manager with packet system dependencies
        _sessionManager = new SessionManager(
            _packetSystem.Factory,
            _packetSystem.Registry,
            logger,
            configuration
        );
    }

    protected override async Task OnStartingAsync(CancellationToken cancellationToken)
    {
        await StartTcpListenerAsync(cancellationToken);
        _gameLoopTask = Task.Run(() => GameLoopAsync(cancellationToken), cancellationToken);
    }

    protected override async Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        await _sessionManager.DisconnectAllAsync(DisconnectReason.ServerShutdown);
        
        if (_gameLoopTask != null)
        {
            try
            {
                await _gameLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }
        
        await StopTcpListenerAsync(cancellationToken);
    }

    private async Task GameLoopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("{ServerName} game loop started at {FPS} FPS", ServerName, TargetFPS);
        
        var stopwatch = Stopwatch.StartNew();
        var lastFrameTime = stopwatch.Elapsed.TotalMilliseconds;

        while (!cancellationToken.IsCancellationRequested)
        {
            var frameStartTime = stopwatch.Elapsed.TotalMilliseconds;
            var deltaTime = frameStartTime - lastFrameTime;
            lastFrameTime = frameStartTime;

            try
            {
                // Process incoming packets (non-blocking)
                await ProcessIncomingPacketsAsync(deltaTime, cancellationToken);

                // Update game logic
                await UpdateGameLogicAsync(deltaTime, cancellationToken);

                // Check heartbeats and disconnect timed-out clients
                await _sessionManager.CheckHeartbeatsAsync();

                // Flush outgoing packets
                await FlushOutgoingPacketsAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in game loop");
            }

            // Sleep to maintain target FPS
            var frameEndTime = stopwatch.Elapsed.TotalMilliseconds;
            var frameElapsed = frameEndTime - frameStartTime;
            var sleepTime = (int)Math.Max(0, TargetFrameTime - frameElapsed);

            if (sleepTime > 0)
            {
                try
                {
                    await Task.Delay(sleepTime, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Logger.LogInformation("{ServerName} game loop stopped", ServerName);
    }

    protected abstract Task StartTcpListenerAsync(CancellationToken cancellationToken);
    protected abstract Task StopTcpListenerAsync(CancellationToken cancellationToken);
    protected abstract Task ProcessIncomingPacketsAsync(double deltaTime, CancellationToken cancellationToken);
    protected abstract Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken);
    protected abstract Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken);
}

