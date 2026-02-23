namespace Login.Server.UseCase;

/// <summary>
/// IPC operations for communicating with character servers.
/// Separated from the server implementation to allow clean DI.
/// </summary>
public interface ICharServerIpcService
{
    Task ForceDisconnectAccountFromCharServersAsync(int accountId, CancellationToken cancellationToken = default);
    Task RequestCharServerAddressSyncAsync(CancellationToken cancellationToken = default);
}
