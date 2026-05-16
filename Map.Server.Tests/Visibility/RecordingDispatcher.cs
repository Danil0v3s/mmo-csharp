using Core.Server.Packets;
using Map.Server.Visibility;

namespace Map.Server.Tests.Visibility;

/// <summary>
/// Test-only <see cref="IPacketDispatcher"/> that records every (session,
/// packet) pair instead of touching a socket. Lets us assert which sessions
/// received which packets without standing up a real <c>SessionManager</c>.
/// </summary>
internal sealed class RecordingDispatcher : IPacketDispatcher
{
    public List<(Guid sessionId, OutgoingPacket packet)> Sent { get; } = new();

    public bool TrySend(Guid sessionId, OutgoingPacket packet)
    {
        Sent.Add((sessionId, packet));
        return true;
    }
}
