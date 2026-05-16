using Core.Server.Network;
using Core.Server.Packets;

namespace Map.Server.Visibility;

public sealed class SessionPacketDispatcher : IPacketDispatcher
{
    private readonly SessionManager _sessions;

    public SessionPacketDispatcher(SessionManager sessions)
    {
        _sessions = sessions;
    }

    public bool TrySend(Guid sessionId, OutgoingPacket packet)
    {
        if (!_sessions.TryGetSession(sessionId, out var session) || session == null)
        {
            return false;
        }
        session.EnqueuePacket(packet);
        return true;
    }
}
