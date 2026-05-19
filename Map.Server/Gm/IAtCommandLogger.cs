using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Gm;

/// <summary>
/// rAthena <c>log_atcommand</c> (log.cpp:295). Persists one row per
/// invocation when the caller's group has <c>LogCommands: true</c>.
/// Fire-and-forget; the persistence layer is async but the game tick
/// doesn't wait on the I/O.
/// </summary>
public interface IAtCommandLogger
{
    void Log(MapSessionData session, PlayerEntity caller, string mapName, string commandLine);
}
