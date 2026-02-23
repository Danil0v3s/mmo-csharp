using Core.Server;

namespace Login.Server;

/// <summary>
/// Provides access to server runtime state.
/// For char server data, use ICharServerRegistry.
/// For char server IPC operations, use ICharServerIpcService.
/// </summary>
public interface ILoginServerRuntime
{
    ServerState State { get; }
}

/// <summary>
/// Holds the login server runtime state.
/// Separate from LoginServerImpl to avoid circular DI dependencies.
/// </summary>
public class LoginServerState : ILoginServerRuntime
{
    private volatile ServerState _state = ServerState.Stopped;

    public ServerState State => _state;

    public void SetState(ServerState state)
    {
        _state = state;
    }
}
