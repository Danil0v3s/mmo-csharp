using Map.Server.Scripting.Records;

namespace Map.Server.Warps;

/// <summary>
/// Runtime catalog of warp portals for every map this server hosts. The
/// service consumes the script registry's <see cref="WarpRegistration"/>
/// entries at boot, marks <see cref="World.CellFlags.NpcTrigger"/> on
/// every cell inside each warp's trigger box (mirroring rAthena's
/// <c>npc_setcells</c>), and exposes O(1) lookup by cell.
///
/// Movement is the only hot-path caller: on each tile-step arrival it
/// checks the cell's <see cref="World.CellFlags.NpcTrigger"/> bit first,
/// and only consults <see cref="TryGetWarpAt"/> when that bit is set.
/// </summary>
public interface IWarpService
{
    /// <summary>
    /// Walk the script registry and build the cell index + trigger-cell
    /// dynamic flags. Idempotent — calling it again rebuilds from scratch
    /// (useful for hot-reload). Must be called after the script bundle
    /// is loaded so <c>INpcRegistry.AllWarps()</c> is populated.
    /// </summary>
    void Build();

    /// <summary>
    /// Returns the warp whose trigger box covers (<paramref name="x"/>,
    /// <paramref name="y"/>) on <paramref name="mapName"/>, or null if no
    /// warp is registered there.
    /// </summary>
    WarpRegistration? TryGetWarpAt(string mapName, short x, short y);

    /// <summary>Total warps loaded across every hosted map.</summary>
    int Count { get; }
}
