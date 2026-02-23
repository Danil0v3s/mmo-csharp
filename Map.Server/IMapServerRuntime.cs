using Core.Server;

namespace Map.Server;

/// <summary>
/// Read-only server state. Safe to inject into handlers without circular dependencies.
/// For IPC operations, use ICharServerIpcService.
/// For player tracking, use IPlayerMapService.
/// </summary>
public interface IMapServerState
{
    ServerState State { get; }
}
