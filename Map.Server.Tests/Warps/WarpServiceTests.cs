using Map.Server.Scripting;
using Map.Server.Scripting.Records;
using Map.Server.Warps;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Warps;

public class WarpServiceTests
{
    [Fact]
    public void Build_MarksEveryCellInTriggerBox()
    {
        // 10×10 fully walkable map.
        var map = new MapData("prontera", 10, 10, new byte[100]);
        var world = new StubWorld(map);

        // Single warp at (5,5) with span 1 → 3×3 trigger box.
        var warp = NewWarp("prontera", 5, 5, 1, 1, "geffen", 100, 100);
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
    public void Build_ZeroSpan_MarksSingleCell()
    {
        var map = new MapData("prontera", 10, 10, new byte[100]);
        var world = new StubWorld(map);
        var warp = NewWarp("prontera", 5, 5, 0, 0, "geffen", 100, 100);
        var service = BuildService(world, new[] { warp });

        Assert.True(map.HasNpcTrigger(5, 5));
        Assert.Same(warp, service.TryGetWarpAt("prontera", 5, 5));
        Assert.False(map.HasNpcTrigger(4, 5));
        Assert.False(map.HasNpcTrigger(5, 4));
    }

    [Fact]
    public void Build_SkipsNonWalkableCellsInBox()
    {
        // 10×10 map: column x=5 is blocked (gat=1). Everything else walkable.
        var cells = new byte[100];
        for (var y = 0; y < 10; y++) cells[y * 10 + 5] = 1;
        var map = new MapData("prontera", 10, 10, cells);
        var world = new StubWorld(map);

        var warp = NewWarp("prontera", 5, 5, 1, 1, "geffen", 100, 100);
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
    public void Build_OutOfBoundsCellsAreSilentlyIgnored()
    {
        // 5×5 map, warp at (0,0) with span 2 → box spans (-2..2)×(-2..2).
        var map = new MapData("prontera", 5, 5, new byte[25]);
        var world = new StubWorld(map);
        var warp = NewWarp("prontera", 0, 0, 2, 2, "geffen", 100, 100);
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
        var warp = NewWarp("prontera", 2, 2, 0, 0, "geffen", 1, 1);
        var service = BuildService(world, new[] { warp });

        Assert.Null(service.TryGetWarpAt("geffen", 2, 2));
        Assert.Null(service.TryGetWarpAt("prontera", 0, 0));
    }

    [Fact]
    public void Build_SkipsWarpsForUnhostedMaps()
    {
        var prontera = new MapData("prontera", 5, 5, new byte[25]);
        var world = new StubWorld(prontera); // geffen not hosted
        var warps = new[]
        {
            NewWarp("prontera", 2, 2, 0, 0, "izlude", 1, 1),
            NewWarp("geffen",   2, 2, 0, 0, "izlude", 1, 1),
        };
        var service = BuildService(world, warps);

        // Only the prontera warp counts because geffen isn't a hosted map.
        Assert.Equal(1, service.Count);
        Assert.NotNull(service.TryGetWarpAt("prontera", 2, 2));
    }

    // ---- helpers ----

    private static WarpRegistration NewWarp(
        string fromMap, short fromX, short fromY, short xs, short ys,
        string toMap, short toX, short toY)
        => new()
        {
            FromMap = fromMap, FromX = fromX, FromY = fromY,
            AreaXs = xs, AreaYs = ys,
            ToMap = toMap, ToX = toX, ToY = toY,
        };

    private static WarpService BuildService(StubWorld world, IEnumerable<WarpRegistration> warps)
    {
        var registry = new NpcRegistry();
        foreach (var w in warps) registry.AddWarp(w);
        var svc = new WarpService(registry, world, NullLogger<WarpService>.Instance);
        svc.Build();
        return svc;
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
}
