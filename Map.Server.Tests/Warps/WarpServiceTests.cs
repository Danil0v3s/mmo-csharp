using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Warps;
using Map.Server.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Warps;

public class WarpServiceTests
{
    [Fact]
    public void Load_MarksEveryCellInTriggerBox()
    {
        // 10×10 fully walkable map.
        var map = new MapData("prontera", 10, 10, new byte[100]);
        var world = new StubWorld(map);

        // Single warp at (5,5) with span 1 → 3×3 trigger box.
        var warp = new WarpEntity
        {
            WarpId = 1, Name = "test_warp",
            SrcMap = "prontera", SrcX = 5, SrcY = 5, SpanXs = 1, SpanYs = 1,
            DstMap = "geffen", DstX = 100, DstY = 100,
        };
        var service = BuildService(world, new[] { warp });

        for (short y = 4; y <= 6; y++)
        for (short x = 4; x <= 6; x++)
        {
            Assert.True(map.HasNpcTrigger(x, y), $"cell ({x},{y}) should be marked");
            Assert.Same(warp, service.TryGetWarpAt("prontera", x, y));
        }

        // Adjacent cells outside the box are clean.
        Assert.False(map.HasNpcTrigger(3, 5));
        Assert.False(map.HasNpcTrigger(7, 5));
        Assert.False(map.HasNpcTrigger(5, 3));
        Assert.False(map.HasNpcTrigger(5, 7));
        Assert.Null(service.TryGetWarpAt("prontera", 3, 5));
    }

    [Fact]
    public void Load_ZeroSpan_MarksSingleCell()
    {
        var map = new MapData("prontera", 10, 10, new byte[100]);
        var world = new StubWorld(map);
        var warp = new WarpEntity
        {
            WarpId = 1, Name = "single_cell",
            SrcMap = "prontera", SrcX = 5, SrcY = 5, SpanXs = 0, SpanYs = 0,
            DstMap = "geffen", DstX = 100, DstY = 100,
        };
        var service = BuildService(world, new[] { warp });

        Assert.True(map.HasNpcTrigger(5, 5));
        Assert.Same(warp, service.TryGetWarpAt("prontera", 5, 5));
        Assert.False(map.HasNpcTrigger(4, 5));
        Assert.False(map.HasNpcTrigger(5, 4));
    }

    [Fact]
    public void Load_SkipsNonWalkableCellsInBox()
    {
        // 10×10 map: column x=5 is blocked (gat=1). Everything else walkable.
        var cells = new byte[100];
        for (var y = 0; y < 10; y++) cells[y * 10 + 5] = 1;
        var map = new MapData("prontera", 10, 10, cells);
        var world = new StubWorld(map);

        var warp = new WarpEntity
        {
            WarpId = 1, Name = "across_wall",
            SrcMap = "prontera", SrcX = 5, SrcY = 5, SpanXs = 1, SpanYs = 1,
            DstMap = "geffen", DstX = 100, DstY = 100,
        };
        BuildService(world, new[] { warp });

        // Walkable cells in the box (rows 4..6, cols 4 and 6) are marked.
        for (short y = 4; y <= 6; y++)
        {
            Assert.True(map.HasNpcTrigger(4, y), $"({4},{y}) walkable in box");
            Assert.True(map.HasNpcTrigger(6, y), $"({6},{y}) walkable in box");
            Assert.False(map.HasNpcTrigger(5, y), $"({5},{y}) blocked → skip per CELL_CHKNOPASS");
        }
    }

    [Fact]
    public void Load_OutOfBoundsCellsAreSilentlyIgnored()
    {
        // 5×5 map, warp at (0,0) with span 2 → box spans (-2..2)×(-2..2).
        // rAthena's npc_setcells lets map_setcell bounds-check; ours does too.
        var map = new MapData("prontera", 5, 5, new byte[25]);
        var world = new StubWorld(map);
        var warp = new WarpEntity
        {
            WarpId = 1, Name = "corner",
            SrcMap = "prontera", SrcX = 0, SrcY = 0, SpanXs = 2, SpanYs = 2,
            DstMap = "geffen", DstX = 100, DstY = 100,
        };
        var service = BuildService(world, new[] { warp });

        // In-bounds quarter (0..2)×(0..2) marked; negatives silently dropped.
        for (short y = 0; y <= 2; y++)
        for (short x = 0; x <= 2; x++)
        {
            Assert.True(map.HasNpcTrigger(x, y));
            Assert.Same(warp, service.TryGetWarpAt("prontera", x, y));
        }
    }

    [Fact]
    public void TryGetWarpAt_WrongMap_ReturnsNull()
    {
        var map = new MapData("prontera", 5, 5, new byte[25]);
        var world = new StubWorld(map);
        var warp = new WarpEntity
        {
            WarpId = 1, Name = "x",
            SrcMap = "prontera", SrcX = 2, SrcY = 2, SpanXs = 0, SpanYs = 0,
            DstMap = "geffen", DstX = 1, DstY = 1,
        };
        var service = BuildService(world, new[] { warp });

        Assert.Null(service.TryGetWarpAt("geffen", 2, 2));
        Assert.Null(service.TryGetWarpAt("prontera", 0, 0));
    }

    [Fact]
    public void Load_SkipsWarpsForUnhostedMaps()
    {
        var prontera = new MapData("prontera", 5, 5, new byte[25]);
        var world = new StubWorld(prontera); // geffen not hosted
        var warps = new[]
        {
            new WarpEntity { WarpId = 1, Name = "p", SrcMap = "prontera", SrcX = 2, SrcY = 2,
                DstMap = "izlude", DstX = 1, DstY = 1 },
            new WarpEntity { WarpId = 2, Name = "g", SrcMap = "geffen", SrcX = 2, SrcY = 2,
                DstMap = "izlude", DstX = 1, DstY = 1 },
        };
        var service = BuildService(world, warps);

        // Only the prontera warp counts because geffen isn't in the registry —
        // GetBySrcMapAsync never gets called for it.
        Assert.Equal(1, service.Count);
        Assert.NotNull(service.TryGetWarpAt("prontera", 2, 2));
    }

    // ---- helpers ----

    private static WarpService BuildService(StubWorld world, IEnumerable<WarpEntity> warps)
    {
        var repo = new StubWarpRepository(warps);
        var sp = new ServiceCollection()
            .AddScoped<IWarpRepository>(_ => repo)
            .BuildServiceProvider();
        return new WarpService(sp.GetRequiredService<IServiceScopeFactory>(), world, NullLogger<WarpService>.Instance);
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _byName;
        public StubWorld(params MapData[] maps) =>
            _byName = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _byName.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _byName.Values;
        public int TotalCells => _byName.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _byName.ContainsKey(name);
    }

    private sealed class StubWarpRepository : IWarpRepository
    {
        private readonly List<WarpEntity> _rows;
        public StubWarpRepository(IEnumerable<WarpEntity> rows) => _rows = rows.ToList();
        public Task<List<WarpEntity>> GetBySrcMapAsync(string mapName, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(w => w.SrcMap == mapName).ToList());
        public Task<List<WarpEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<WarpEntity>(_rows));
    }
}
