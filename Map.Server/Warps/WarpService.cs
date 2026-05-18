using Map.Server.Scripting;
using Map.Server.Scripting.Records;
using Map.Server.World;

namespace Map.Server.Warps;

/// <summary>
/// Default <see cref="IWarpService"/>. <see cref="Build"/> walks every
/// <see cref="WarpRegistration"/> in <see cref="INpcRegistry.AllWarps"/>,
/// marks <see cref="CellFlags.NpcTrigger"/> on every walkable cell in
/// each warp's trigger box — mirrors rAthena <c>npc_setcells</c>
/// (npc.cpp:4943–4972), which skips cells where <c>CELL_CHKNOPASS</c>
/// holds — and builds a flat <c>Dictionary&lt;(map, x, y), WarpRegistration&gt;</c>
/// for O(1) lookup by cell.
///
/// Two-phase init: constructor takes deps only; <see cref="Build"/> is
/// invoked from <c>MapServerImpl.StartAsync</c> after the scripting
/// bundle is loaded so the registry is populated.
/// </summary>
public sealed class WarpService : IWarpService
{
    private readonly INpcRegistry _registry;
    private readonly IMapWorldRegistry _world;
    private readonly ILogger<WarpService> _logger;
    private Dictionary<CellKey, WarpRegistration> _byCell = new();
    private int _warpCount;

    public WarpService(
        INpcRegistry registry,
        IMapWorldRegistry world,
        ILogger<WarpService> logger)
    {
        _registry = registry;
        _world = world;
        _logger = logger;
    }

    public WarpRegistration? TryGetWarpAt(string mapName, short x, short y) =>
        _byCell.TryGetValue(new CellKey(mapName, x, y), out var warp) ? warp : null;

    public int Count => _warpCount;

    public void Build()
    {
        var byCell = new Dictionary<CellKey, WarpRegistration>();
        var hostedWarps = 0;
        var triggerCells = 0;
        var skippedNonWalkable = 0;

        foreach (var grp in _registry.AllWarps().GroupBy(w => w.FromMap))
        {
            var map = _world.Get(grp.Key);
            if (map == null) continue;  // unhosted map — silently skip

            foreach (var warp in grp)
            {
                hostedWarps++;
                MarkTriggerBox(map, warp, byCell, ref triggerCells, ref skippedNonWalkable);
            }
        }

        _byCell = byCell;
        _warpCount = hostedWarps;

        _logger.LogInformation(
            "WarpService loaded {Warps} warps across {Maps} hosted maps ({TriggerCells} trigger cells, {Skipped} non-walkable skipped)",
            hostedWarps, _world.All.Count(), triggerCells, skippedNonWalkable);
    }

    private static void MarkTriggerBox(
        MapData map,
        WarpRegistration warp,
        Dictionary<CellKey, WarpRegistration> byCell,
        ref int triggerCells,
        ref int skippedNonWalkable)
    {
        // rAthena npc.cpp:4965-4971 — inclusive (y-ys..y+ys) × (x-xs..x+xs).
        // xs/ys are half-extents; xs=ys=0 means a single-cell trigger.
        for (var i = warp.FromY - warp.AreaYs; i <= warp.FromY + warp.AreaYs; i++)
        {
            for (var j = warp.FromX - warp.AreaXs; j <= warp.FromX + warp.AreaXs; j++)
            {
                var cx = (short)j;
                var cy = (short)i;

                // CELL_CHKNOPASS gate: skip non-walkable cells. rAthena
                // does this so cliffs / walls inside an OnTouch box don't
                // fire when the player can't actually stand there.
                if (!map.IsWalkable(cx, cy))
                {
                    skippedNonWalkable++;
                    continue;
                }

                map.SetDynamicFlag(cx, cy, CellFlags.NpcTrigger, true);

                var key = new CellKey(map.Name, cx, cy);
                // Last-writer wins when two warps overlap (rare; data bug).
                // Doesn't matter for our purpose since both would teleport
                // the player — the dispatcher picks the one indexed here.
                byCell[key] = warp;
                triggerCells++;
            }
        }
    }

    private readonly record struct CellKey(string Map, short X, short Y);
}
