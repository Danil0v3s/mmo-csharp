using Core.Server;

namespace Login.Server.UseCase;

/// <summary>
/// Provides access to server runtime state.
/// For char server data, use ICharServerRegistry.
/// For char server IPC operations, use ICharServerIpcService.
/// </summary>
public interface ILoginServerRuntime
{
    ServerState State { get; }
}
