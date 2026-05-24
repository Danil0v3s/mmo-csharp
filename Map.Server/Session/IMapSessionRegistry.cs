using Core.Server.Network;
using Map.Server.Entities;

namespace Map.Server.Session;

/// <summary>
/// Lightweight session-lookup helper. Wraps <see cref="SessionManager"/>
/// so gameplay services can resolve from <see cref="PlayerEntity"/> →
/// <see cref="MapSessionData"/> without pulling in the full network
/// API.
///
/// <para>The session id on <see cref="PlayerEntity.SessionId"/> is the
/// rAthena <c>sd-&gt;status.account_id</c>'s C# proxy — it's the GUID
/// the TCP layer assigned, set by the connect handler when the PC's
/// session was bootstrapped.</para>
/// </summary>
public interface IMapSessionRegistry
{
    /// <summary>
    /// Resolve the session that owns <paramref name="pc"/>, or null if
    /// the player isn't currently connected (transient between
    /// disconnect and entity removal).
    /// </summary>
    MapSessionData? TryGet(PlayerEntity pc);

    /// <summary>Look up by session GUID directly.</summary>
    MapSessionData? TryGet(Guid sessionId);
}
