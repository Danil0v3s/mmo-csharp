using Core.Server.Network;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;

namespace Map.Server.Session;

/// <summary>
/// Drives the spawn/despawn side-effects that the session manager doesn't
/// know about. The TCP session machinery in <c>SessionManager</c> is
/// gameplay-agnostic; it sets <c>IsAlive=false</c> and eventually disposes
/// the socket, but it doesn't tear down the player's <see cref="PlayerEntity"/>,
/// broadcast <c>ZC_NOTIFY_VANISH</c>, or fire the char-server LeaveMap IPC.
///
/// This service polls dead sessions on the map tick (cheap — only iterates
/// once per session), runs the cleanup exactly once per session via the
/// <see cref="MapSessionData.CleanupCompleted"/> flag, and lets the
/// <c>SessionManager</c>'s own cleanup loop reap the socket afterwards.
/// </summary>
public sealed class MapSessionLifecycle
{
    private readonly SessionManager _sessions;
    private readonly IEntityRegistry _entities;
    private readonly IVisibilityService _visibility;
    private readonly ICharServerIpcService _charServerIpc;
    private readonly ILogger<MapSessionLifecycle> _logger;

    public MapSessionLifecycle(
        SessionManager sessions,
        IEntityRegistry entities,
        IVisibilityService visibility,
        ICharServerIpcService charServerIpc,
        ILogger<MapSessionLifecycle> logger)
    {
        _sessions = sessions;
        _entities = entities;
        _visibility = visibility;
        _charServerIpc = charServerIpc;
        _logger = logger;
    }

    /// <summary>
    /// Sweep dead <see cref="MapSessionData"/> sessions and run cleanup. Safe
    /// to call every tick; idempotent per session.
    /// </summary>
    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        foreach (var session in _sessions.GetAllSessions())
        {
            if (session is not MapSessionData map) continue;
            if (map.IsAlive) continue;
            if (map.CleanupCompleted) continue;

            await CleanupAsync(map, cancellationToken);
            map.CleanupCompleted = true;
        }
    }

    private async Task CleanupAsync(MapSessionData session, CancellationToken cancellationToken)
    {
        var entity = session.EntityId is { } eid ? _entities.Get(eid) : null;
        if (entity != null)
        {
            try
            {
                _visibility.NotifyVanishedToArea(entity, VanishReason.Logout);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VanishedToArea broadcast failed for char {CharId}", session.CharacterId);
            }
            _entities.Remove(entity.Id);
        }

        if (session.AccountId is { } accountId && session.CharacterId is { } charId)
        {
            try
            {
                await _charServerIpc.SaveCharacterStateAsync(
                    accountId,
                    charId,
                    setOfflineAfterSave: false,
                    finalSave: true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Final SaveCharacterState failed for char {CharId}", charId);
            }

            try
            {
                await _charServerIpc.SetCharacterOfflineAsync(accountId, charId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SetCharacterOffline failed for char {CharId}", charId);
            }
        }

        _logger.LogInformation(
            "Cleaned up session {SessionId} (char {CharId})",
            session.SessionId, session.CharacterId);
    }
}
