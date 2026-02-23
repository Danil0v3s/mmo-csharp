using Core.Server;

namespace Char.Server;

/// <summary>
/// Holds the char server registration state.
/// Implements ICharServerState for safe injection into handlers.
/// </summary>
public class CharServerState : ICharServerState
{
    private volatile ServerState _state = ServerState.Stopped;
    private volatile bool _registeredToLoginServer;
    private volatile int _registeredServerId = -1;

    public ServerState State => _state;
    public bool IsRegisteredToLoginServer => _registeredToLoginServer;
    public int RegisteredServerId => _registeredServerId;

    public void SetState(ServerState state) => _state = state;
    public void SetRegistered(bool registered, int serverId)
    {
        _registeredToLoginServer = registered;
        _registeredServerId = serverId;
    }
}
