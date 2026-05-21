using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobRandomWalkService"/>. Picks a random cell
/// within ±<see cref="IMobRandomWalkService.MaxWanderRadius"/> of the
/// mob's current position and queues a walk via
/// <see cref="IMovementService"/>. rAthena mob.cpp:1673.
///
/// <para>We approximate the rAthena "try the 7th cell in randomized
/// direction" anti-clutter loop with a simple "pick random offset,
/// try once, fall back to nothing." The cell-passability check is
/// delegated to <see cref="IMovementService.TryStartWalk"/>; if it
/// returns false we treat the wander as failed and the AI ticker
/// will try again next tick.</para>
/// </summary>
public sealed class MobRandomWalkService : IMobRandomWalkService
{
    private readonly IMovementService? _movement;
    private readonly Random _rng;
    private readonly ILogger<MobRandomWalkService> _logger;

    public MobRandomWalkService(
        ILogger<MobRandomWalkService> logger,
        IMovementService? movement = null,
        Random? rng = null)
    {
        _movement = movement;
        _rng = rng ?? Random.Shared;
        _logger = logger;
    }

    public bool TryWander(MobEntity mob, long nowTick)
    {
        // rAthena mob.cpp:1681 — first-time init: set NextWanderTick.
        if (mob.NextWanderTick == 0)
        {
            mob.NextWanderTick = nowTick
                + IMobRandomWalkService.MinWalkIntervalMs
                + _rng.Next(1000);
            return false; // returns 1 in rAthena but no walk happens
        }

        // rAthena mob.cpp:1686-1690 — guards.
        if (mob.NextWanderTick > nowTick) return false;
        if ((mob.Stats.Mode & MobMode.NoRandomWalk) != 0) return false;
        if ((mob.Stats.Mode & MobMode.CanMove) == 0) return false;

        // Pick a random nearby cell. rAthena uses a 15x15 grid search;
        // we pick once and rely on the movement service to reject if
        // the cell is impassable.
        var d = IMobRandomWalkService.MaxWanderRadius;
        var dx = _rng.Next(-d, d + 1);
        var dy = _rng.Next(-d, d + 1);
        // Avoid same-cell wander (rAthena guard).
        if (dx == 0 && dy == 0) dx = 1;

        var targetX = (short)Math.Clamp(mob.X + dx, 0, short.MaxValue);
        var targetY = (short)Math.Clamp(mob.Y + dy, 0, short.MaxValue);

        // Schedule next wander before issuing the walk so a movement
        // failure doesn't cause a tick-storm.
        mob.NextWanderTick = nowTick
            + IMobRandomWalkService.MinWalkIntervalMs
            + _rng.Next(1000);

        if (_movement == null) return false; // canonical surface, no impl
        var ok = _movement.TryStartWalk(mob, targetX, targetY);
        if (ok)
            _logger.LogTrace("mob {Mob} wander → ({X},{Y})", mob.Id, targetX, targetY);
        return ok;
    }
}
