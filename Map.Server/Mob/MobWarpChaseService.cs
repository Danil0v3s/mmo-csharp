using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Canonical entry point for rAthena <c>mob_warpchase</c>
/// (mob.cpp:1776). The full impl scans
/// <c>map_foreachinallrange</c> for warp NPCs whose
/// <c>u.warp.mapindex</c> matches the target's map and picks the
/// closest one.
///
/// <para><b>Data-pending:</b> our <see cref="NpcEntity"/> doesn't yet
/// carry the warp subtype (<c>NPCTYPE_WARP</c>) or its destination
/// map/x/y. Until the NPC subtype lands the only honest answer is
/// "no warp found." We still apply rAthena's outer gates
/// (target-on-same-map short-circuits, no target → no chase) so the
/// AI ticker doesn't waste a scan, and the canonical interface is in
/// place so a future warp-NPC port can flip the return without
/// touching call sites.</para>
/// </summary>
public sealed class MobWarpChaseService : IMobWarpChaseService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<MobWarpChaseService> _logger;

    public MobWarpChaseService(IEntityRegistry entities, ILogger<MobWarpChaseService> logger)
    {
        _entities = entities;
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

        // Data-pending: scan would walk _entities for warp NPCs with a
        // destination matching target.MapId. NpcEntity doesn't carry warp
        // subtype yet, so the scan is a no-op. The interface still exists
        // so callers can plug into a real impl later.
        _logger.LogDebug(
            "warpchase: mob {Mob} wants to follow target on map {Map} (no warp NPCs registered yet)",
            mob.Id, target.MapId);
        return WarpChaseResult.NotApplicable;
    }
}
