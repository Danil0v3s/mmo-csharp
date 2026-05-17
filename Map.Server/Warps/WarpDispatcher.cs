using Core.Database.Entities;
using Map.Server.Entities;

namespace Map.Server.Warps;

/// <summary>
/// MS1 stub <see cref="IWarpDispatcher"/> — confirms the cell-flag dispatch
/// fires by logging the destination. Does NOT yet teleport the player;
/// the full <c>pc_setpos</c> cascade (saved-coord update, map re-auth
/// for cross-server destinations, ZC_NPCACK_MAPMOVE for cross-map,
/// ZC_TELEPORT for same-map, view-range clear + STANDENTRY of new view)
/// lands in its own slice.
///
/// rAthena reference: <c>npc_touch_areanpc</c> (npc.cpp:1862) for the
/// gate; <c>pc_setpos</c> (pc.cpp:6949) for the actual move.
///
/// Walk cancellation is handled upstream in
/// <see cref="Movement.MovementService"/> before the dispatcher fires
/// (representing what <c>pc_setpos</c> does to <c>ud->walktimer</c>), so
/// the dispatcher does not depend on the movement service — that would
/// produce a DI cycle.
/// </summary>
public sealed class WarpDispatcher : IWarpDispatcher
{
    private readonly ILogger<WarpDispatcher> _logger;

    public WarpDispatcher(ILogger<WarpDispatcher> logger)
    {
        _logger = logger;
    }

    public void OnEnterWarp(PlayerEntity entity, WarpEntity warp)
    {
        _logger.LogInformation(
            "Warp trigger: char {Char} entered '{WarpName}' on {SrcMap} ({Sx},{Sy}) → {DstMap} ({Dx},{Dy}) — teleport pending pc_setpos port",
            entity.Id, warp.Name, warp.SrcMap, entity.X, entity.Y,
            warp.DstMap, warp.DstX, warp.DstY);
    }
}
