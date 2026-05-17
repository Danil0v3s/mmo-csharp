using Core.Database.Entities;

namespace Map.Server.Warps;

/// <summary>
/// Runtime catalog of warp portals for every map this server hosts. The
/// service loads the <c>warp</c> table at boot, marks
/// <see cref="World.CellFlags.NpcTrigger"/> on every cell inside each
/// warp's trigger box (mirroring rAthena's <c>npc_setcells</c>), and
/// exposes O(1) lookup by cell.
///
/// Movement is the only hot-path caller: on each tile-step arrival it
/// checks the cell's <see cref="World.CellFlags.NpcTrigger"/> bit first,
/// and only consults <see cref="TryGetWarpAt"/> when that bit is set.
/// </summary>
public interface IWarpService
{
    /// <summary>
    /// Returns the warp whose trigger box covers (<paramref name="x"/>,
    /// <paramref name="y"/>) on <paramref name="mapName"/>, or null if no
    /// warp is registered there.
    /// </summary>
    WarpEntity? TryGetWarpAt(string mapName, short x, short y);

    /// <summary>Total warps loaded across every hosted map.</summary>
    int Count { get; }
}
