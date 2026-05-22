using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Scripting;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Canonical entry point for rAthena <c>mob_warpchase</c>
/// (mob.cpp:1776). Scans <see cref="INpcRegistry.AllWarps"/> for
/// warps whose source map matches the mob's and whose destination
/// map matches the target's, then walks the mob toward the closest
/// such warp.
///
/// <para>T5.1c — replaces the T4.9c data-pending stub now that
/// `registerWarp` data is the canonical source of truth for warp
/// destinations. We don't yet have a separate NpcEntity warp
/// subtype; the WarpRegistration records that
/// <see cref="Warps.IWarpService"/> consumes already carry the
/// (FromMap, FromX, FromY, ToMap, ToX, ToY) tuple we need.</para>
/// </summary>
public sealed class MobWarpChaseService : IMobWarpChaseService
{
    private readonly IEntityRegistry _entities;
    private readonly INpcRegistry _npcs;
    private readonly IMovementService? _movement;
    private readonly ILogger<MobWarpChaseService> _logger;

    public MobWarpChaseService(
        IEntityRegistry entities,
        INpcRegistry npcs,
        ILogger<MobWarpChaseService> logger,
        IMovementService? movement = null)
    {
        _entities = entities;
        _npcs = npcs;
        _movement = movement;
        _logger = logger;
    }

    /// <inheritdoc/>
    public WarpChaseResult TryWarpChase(MobEntity mob, Entity target)
    {
        // rAthena mob.cpp:1781-1785 — null / mapless target rejects.
        if (target == null) return WarpChaseResult.NotApplicable;

        // mob.cpp:1796 — same map AND already close enough → no chase needed.
        // We don't have CELL_CHKNPC walk-target inspection so we apply just
        // the same-map gate; the in-range short-circuit is the AttackService's
        // job anyway.
        if (target.MapId == mob.MapId) return WarpChaseResult.NotApplicable;

        // T5.1c — scan registered warps. We compare against the
        // mob.MapId (uint hash of map name) by hashing each warp's
        // FromMap / ToMap. This mirrors EntityRegistry's name-to-id
        // convention so a future MapIndex layer flips both call sites
        // in one shot.
        WarpScripting? closest = null;
        int closestDist = int.MaxValue;
        foreach (var warp in _npcs.AllWarps())
        {
            if ((uint)warp.FromMap.GetHashCode() != mob.MapId) continue;
            if ((uint)warp.ToMap.GetHashCode() != target.MapId) continue;
            var dist = Math.Max(Math.Abs(warp.FromX - mob.X), Math.Abs(warp.FromY - mob.Y));
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = new WarpScripting(warp.FromX, warp.FromY);
            }
        }

        if (closest == null)
        {
            _logger.LogDebug(
                "warpchase: mob {Mob} found no warp from map {From} to map {To}",
                mob.Id, mob.MapId, target.MapId);
            return WarpChaseResult.NotApplicable;
        }

        // Movement service may be absent in unit tests; the canonical
        // surface is the scan + decision. Real engines walk the mob.
        if (_movement == null) return WarpChaseResult.NotApplicable;
        if (_movement.TryStartWalk(mob, closest.Value.X, closest.Value.Y))
        {
            _logger.LogDebug(
                "warpchase: mob {Mob} walking to ({X},{Y}) — closest warp to target map {To}",
                mob.Id, closest.Value.X, closest.Value.Y, target.MapId);
            return WarpChaseResult.Walking;
        }
        return WarpChaseResult.NotApplicable;
    }

    private readonly record struct WarpScripting(short X, short Y);
}
