namespace Core.Server;

public interface IServer
{
    string ServerName { get; }
    ServerState State { get; }
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
}

public enum ServerState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

