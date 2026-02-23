using Core.Server;

namespace Map.Server;

/// <summary>
/// Holds the map server state.
/// </summary>
public class MapServerState : IMapServerState
{
    private volatile ServerState _state = ServerState.Stopped;

    public ServerState State => _state;

    public void SetState(ServerState state) => _state = state;
}
