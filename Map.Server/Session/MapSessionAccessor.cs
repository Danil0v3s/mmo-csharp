using Core.Server.Network;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Session;

// `MapSessionData` lives in the root `Map.Server` namespace — pull it in
// so consumers of this file don't need a fully-qualified reference.
using MapSessionData = Map.Server.MapSessionData;

/// <summary>
/// Bridges <see cref="IExpService"/> (and any future status-broadcast
/// caller) to the live <see cref="SessionManager"/> without taking a
/// hard dep on the Core.Server type. The lookup is O(N) over active
/// sessions — fine for the player counts we target; can index by
/// EntityId later if it shows up in a profile.
/// </summary>
public sealed class MapSessionAccessor : ISessionManagerAccessor
{
    private readonly SessionManager _sessionManager;

    public MapSessionAccessor(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public MapSessionData? GetByEntityId(EntityId entityId)
    {
        foreach (var session in _sessionManager.GetAllSessions())
        {
            if (session is MapSessionData m && m.EntityId == entityId)
                return m;
        }
        return null;
    }
}
