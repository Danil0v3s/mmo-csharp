using Map.Server.Entities;

namespace Map.Server.Movement;

/// <summary>
/// Port of rAthena <c>pc_setpos</c> (pc.cpp:6949) — the canonical
/// "move PC to a cell" path, used by warps, GM @warp, savepoint
/// respawn, instance entries, item-warp scrolls, and skill teleport.
///
/// First slice: same-process map changes only. Cross-process warps
/// (different Map.Server hosting the target map) need a re-handoff
/// via char-server auth_node — that lands when multi-map-server
/// configurations come online.
/// </summary>
public interface IPcSetposService
{
    /// <summary>
    /// Move <paramref name="pc"/> to <paramref name="mapName"/>/<paramref name="x"/>/<paramref name="y"/>.
    /// Cancels any in-flight walk / attack, broadcasts vanish on the
    /// old AOI, re-broadcasts STANDENTRY on the new one, sends
    /// <c>ZC_NPCACK_MAPMOVE</c> to the player so the client requests
    /// the new map's tile cache.
    /// </summary>
    SetposResult Setpos(PlayerEntity pc, string mapName, short x, short y);
}

public enum SetposResult
{
    Ok,
    UnknownMap,
    NotWalkable,
    OffMap,
}
