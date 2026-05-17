using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.World;
using Microsoft.Extensions.DependencyInjection;

namespace Map.Server.Warps;

/// <summary>
/// Default <see cref="IWarpService"/>. At construction:
/// <list type="number">
///   <item>Pulls every warp row for the maps this server hosts via
///     <see cref="IWarpRepository"/>.</item>
///   <item>For each warp, walks the trigger box
///     <c>(x ± xs, y ± ys)</c> and marks
///     <see cref="CellFlags.NpcTrigger"/> on each walkable cell —
///     mirrors rAthena <c>npc_setcells</c> (npc.cpp:4943–4972), which
///     skips cells where <c>CELL_CHKNOPASS</c> holds.</item>
///   <item>Builds a flat <c>Dictionary&lt;(map, x, y), WarpEntity&gt;</c>
///     for O(1) trigger-box → warp lookup.</item>
/// </list>
///
/// Singleton + scoped repository: takes <see cref="IServiceScopeFactory"/>
/// to avoid the captive-dependency lifecycle issue (matches the
/// <see cref="Mob.MobDb"/> pattern).
/// </summary>
public sealed class WarpService : IWarpService
{
    private readonly Dictionary<CellKey, WarpEntity> _byCell;
    private readonly int _warpCount;

    public WarpService(
        IServiceScopeFactory scopeFactory,
        IMapWorldRegistry world,
        ILogger<WarpService> logger)
    {
        (_byCell, _warpCount) = Load(scopeFactory, world, logger);
    }

    public WarpEntity? TryGetWarpAt(string mapName, short x, short y) =>
        _byCell.TryGetValue(new CellKey(mapName, x, y), out var warp) ? warp : null;

    public int Count => _warpCount;

    private static (Dictionary<CellKey, WarpEntity> ByCell, int WarpCount) Load(
        IServiceScopeFactory scopeFactory,
        IMapWorldRegistry world,
        ILogger<WarpService> logger)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWarpRepository>();

        var byCell = new Dictionary<CellKey, WarpEntity>();
        var hostedWarps = 0;
        var triggerCells = 0;
        var skippedNonWalkable = 0;

        foreach (var map in world.All)
        {
            // GetAwaiter().GetResult() is intentional: this runs once at
            // boot, not on the tick. Per-map queries (instead of GetAll)
            // mean we only fetch what this server actually hosts.
            var warps = repo.GetBySrcMapAsync(map.Name).GetAwaiter().GetResult();
            if (warps.Count == 0) continue;

            foreach (var warp in warps)
            {
                hostedWarps++;
                MarkTriggerBox(map, warp, byCell, ref triggerCells, ref skippedNonWalkable);
            }
        }

        logger.LogInformation(
            "WarpService loaded {Warps} warps across {Maps} hosted maps ({TriggerCells} trigger cells, {Skipped} non-walkable skipped)",
            hostedWarps, world.All.Count(), triggerCells, skippedNonWalkable);
        return (byCell, hostedWarps);
    }

    private static void MarkTriggerBox(
        MapData map,
        WarpEntity warp,
        Dictionary<CellKey, WarpEntity> byCell,
        ref int triggerCells,
        ref int skippedNonWalkable)
    {
        // rAthena npc.cpp:4965-4971 — inclusive (y-ys..y+ys) × (x-xs..x+xs).
        // xs/ys are half-extents; xs=ys=0 means a single-cell trigger.
        for (var i = warp.SrcY - warp.SpanYs; i <= warp.SrcY + warp.SpanYs; i++)
        {
            for (var j = warp.SrcX - warp.SpanXs; j <= warp.SrcX + warp.SpanXs; j++)
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
                // Last-writer wins when two warps overlap (rare; would be
                // a data bug). Doesn't matter for our purpose since both
                // would teleport the player — the warp dispatcher just
                // picks the one indexed here.
                byCell[key] = warp;
                triggerCells++;
            }
        }
    }

    private readonly record struct CellKey(string Map, short X, short Y);
}
