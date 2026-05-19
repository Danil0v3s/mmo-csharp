using Map.Server.Entities;

namespace Map.Server.Movement;

/// <summary>
/// Position-related pc.cpp helpers that didn't make it into
/// <see cref="IPcSetposService"/>:
///
/// <list type="bullet">
///   <item><c>pc_lastpoint_special</c> — disallow the
///         <c>last_point</c> save slot on certain maps (mvp rooms,
///         instances, jail). Returns true if the map qualifies.</item>
///   <item><c>pc_randomwarp</c> — warp the PC to a random walkable
///         cell on their current map. Used by some skill effects
///         (e.g. Warp Portal failure).</item>
///   <item><c>pc_memo</c> — memorise the current cell as a warp
///         scroll slot. Stub until warp scroll item-effect ports.</item>
///   <item><c>pc_cell_basilica</c> — Basilica/Land Protector cell
///         filter. Stub until ground-unit cell effects port.</item>
/// </list>
/// </summary>
public interface IPlayerPositionHelpers
{
    bool IsLastpointSpecial(string mapName);
    bool RandomWarp(PlayerEntity pc);
    bool Memo(PlayerEntity pc, int slot);
    bool IsBasilicaCell(PlayerEntity pc);
}
