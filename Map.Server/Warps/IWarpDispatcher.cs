using Core.Database.Entities;
using Map.Server.Entities;

namespace Map.Server.Warps;

/// <summary>
/// Dispatches a warp trigger that a walking entity just stepped onto.
/// Equivalent in role to rAthena <c>npc_touch_areanpc</c> for
/// <c>NPCTYPE_WARP</c> (npc.cpp:1862–1922) — which gates on player state
/// (hidden / dead / rewarp-loop counter) then calls <c>pc_setpos</c>
/// with the warp's destination.
///
/// Kept behind an interface so <see cref="Movement.MovementService"/>
/// doesn't depend on the teleport / map-change implementation — which is
/// its own large port (the full <c>pc_setpos</c> cascade with map
/// auth re-handoff for cross-server warps).
/// </summary>
public interface IWarpDispatcher
{
    /// <summary>Player <paramref name="entity"/> just arrived on a cell
    /// flagged with <see cref="World.CellFlags.NpcTrigger"/>; the resolved
    /// warp row is <paramref name="warp"/>.</summary>
    void OnEnterWarp(PlayerEntity entity, WarpEntity warp);
}
