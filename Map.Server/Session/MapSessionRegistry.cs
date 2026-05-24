using Core.Server.Network;
using Map.Server.Entities;

namespace Map.Server.Session;

/// <summary>
/// Default <see cref="IMapSessionRegistry"/>. Forwards every lookup to
/// the live <see cref="SessionManager"/> singleton, casting the
/// returned <see cref="ClientSession"/> to <see cref="MapSessionData"/>
/// (the only session type the map server ever creates).
/// </summary>
public sealed class MapSessionRegistry : IMapSessionRegistry
{
    private readonly SessionManager _sessions;

    public MapSessionRegistry(SessionManager sessions) => _sessions = sessions;

    public MapSessionData? TryGet(PlayerEntity pc) => TryGet(pc.SessionId);

    public MapSessionData? TryGet(Guid sessionId)
    {
        if (!_sessions.TryGetSession(sessionId, out var session)) return null;
        return session as MapSessionData;
    }
}
