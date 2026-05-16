using Core.Server.Packets;

namespace Map.Server.Visibility;

/// <summary>
/// Thin abstraction over the session manager so visibility / movement
/// broadcast code doesn't depend on the full TCP session machinery and so
/// gameplay tests can capture sent packets in-process.
/// </summary>
public interface IPacketDispatcher
{
    /// <summary>
    /// Enqueue <paramref name="packet"/> on the session identified by
    /// <paramref name="sessionId"/>. Returns true if the session was alive and
    /// the packet got queued, false if the session is unknown or dead.
    /// </summary>
    bool TrySend(Guid sessionId, OutgoingPacket packet);
}
