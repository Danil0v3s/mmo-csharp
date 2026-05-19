using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Concrete <see cref="IStatusChangeService"/>. Stores active SCs in a
/// per-entity dictionary; ticks them all from one game-loop pump.
///
/// Refresh-on-restart matches rAthena's default <c>SCSTART_NOAVOID</c>
/// behavior: re-applying the same SC removes the old one (running the
/// stat-mod revert) and applies a fresh instance. Custom stack rules
/// would slot in via <see cref="StatusEffectHandler"/> if/when needed.
/// </summary>
public sealed class StatusChangeService : IStatusChangeService
{
    private readonly IDamageService _damage;
    private readonly IEntityRegistry _entities;
    private readonly StatusEffectRegistry _effects;
    private readonly ILogger<StatusChangeService> _logger;

    /// <summary>
    /// All active SCs by attached entity. <see cref="StatusType"/> is the
    /// inner dict key so per-type lookup / refresh is O(1).
    /// </summary>
    private readonly Dictionary<EntityId, Dictionary<StatusType, StatusChange>> _active = new();

    public StatusChangeService(
        IDamageService damage,
        IEntityRegistry entities,
        StatusEffectRegistry effects,
        ILogger<StatusChangeService> logger)
    {
        _damage = damage;
        _entities = entities;
        _effects = effects;
        _logger = logger;
    }

    public StatusChange? Start(
        Entity target,
        StatusType type,
        int val1, int val2, int val3, int val4,
        int durationMs,
        Entity? source = null,
        long nowTick = long.MinValue)
    {
        var handler = _effects.Get(type);
        if (handler == null)
        {
            _logger.LogDebug("StatusChange.Start: no handler registered for {Type}", type);
            return null;
        }

        // Refresh-on-restart: end the previous instance so its OnEnd
        // reverts any stat mods before we re-apply.
        End(target, type);

        // Sentinel: int.MinValue (passed by callers that want a default).
        // Negative `nowTick` from a deterministic-time caller is allowed
        // and meaningful; only the explicit sentinel falls back to wall
        // clock so the test/game timebases don't bleed into each other.
        if (nowTick == long.MinValue) nowTick = Environment.TickCount64;
        var sc = new StatusChange
        {
            Type = type,
            Val1 = val1, Val2 = val2, Val3 = val3, Val4 = val4,
            ExpiresAt = durationMs > 0 ? nowTick + durationMs : -1,
            PeriodMs = handler.PeriodMs,
            NextTick = handler.PeriodMs > 0 ? nowTick + handler.PeriodMs : 0,
        };

        if (!_active.TryGetValue(target.Id, out var perEntity))
        {
            perEntity = new Dictionary<StatusType, StatusChange>();
            _active[target.Id] = perEntity;
        }
        perEntity[type] = sc;

        handler.OnStart(target, sc, source);
        return sc;
    }

    public bool End(Entity target, StatusType type)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity)) return false;
        if (!perEntity.Remove(type, out var sc)) return false;

        var handler = _effects.Get(type);
        handler?.OnEnd(target, sc);

        if (perEntity.Count == 0) _active.Remove(target.Id);
        return true;
    }

    public StatusChange? Get(Entity target, StatusType type)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity)) return null;
        return perEntity.GetValueOrDefault(type);
    }

    public void Tick(long nowTick)
    {
        if (_active.Count == 0) return;

        // Snapshot keys: handler callbacks may end the SC (or another
        // SC on the same entity) while we iterate.
        foreach (var (entityId, perEntity) in _active.ToArray())
        {
            var entity = _entities.Get(entityId);
            if (entity == null)
            {
                _active.Remove(entityId);
                continue;
            }

            foreach (var sc in perEntity.Values.ToArray())
            {
                // Periodic first — rAthena fires the tick effect before
                // checking expiry so a final DoT lands on the boundary.
                if (sc.PeriodMs > 0 && sc.NextTick != 0 && nowTick >= sc.NextTick)
                {
                    sc.NextTick = nowTick + sc.PeriodMs;
                    var handler = _effects.Get(sc.Type);
                    handler?.OnPeriodic?.Invoke(entity, sc, dmg => _damage.ApplyDamage(entity, dmg));
                }

                // Expiry. ExpiresAt = -1 means "until manually removed."
                if (sc.ExpiresAt > 0 && nowTick >= sc.ExpiresAt)
                {
                    End(entity, sc.Type);
                }
            }
        }
    }
}
