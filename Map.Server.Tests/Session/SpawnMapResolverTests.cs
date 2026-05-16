using Map.Server.Session;
using Map.Server.World;

namespace Map.Server.Tests.Session;

public class SpawnMapResolverTests
{
    [Fact]
    public void Resolve_PrefersSavedMap_WhenLoaded()
    {
        var world = NewWorld("prontera", "morocc", "payon");
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: new[] { "prontera" }, savedMapName: "morocc");

        Assert.NotNull(map);
        Assert.Equal("morocc", map!.Name);
    }

    [Fact]
    public void Resolve_TolerantOfGatSuffix()
    {
        var world = NewWorld("prontera");
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: new[] { "prontera" }, savedMapName: "prontera.gat");

        Assert.Equal("prontera", map!.Name);
    }

    [Fact]
    public void Resolve_FallsBackToConfiguredMap_WhenSavedNotLoaded()
    {
        var world = NewWorld("prontera", "payon");
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: new[] { "prontera" }, savedMapName: "lutie");

        Assert.Equal("prontera", map!.Name);
    }

    [Fact]
    public void Resolve_FallsBackToFirstLoadedMap_WhenConfiguredListEmpty()
    {
        var world = NewWorld("prontera", "payon");
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: Array.Empty<string>(), savedMapName: null);

        Assert.NotNull(map);
        // Either is acceptable as long as it's loaded.
        Assert.Contains(map!.Name, new[] { "prontera", "payon" });
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNothingLoaded()
    {
        var world = NewWorld();
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: new[] { "prontera" }, savedMapName: "prontera");

        Assert.Null(map);
    }

    [Fact]
    public void Resolve_IgnoresEmptySavedName()
    {
        var world = NewWorld("prontera");
        var map = SpawnMapResolver.Resolve(
            world, configuredMaps: new[] { "prontera" }, savedMapName: "");

        Assert.Equal("prontera", map!.Name);
    }

    private static IMapWorldRegistry NewWorld(params string[] names)
    {
        var maps = names.Select(n => new MapData(n, 100, 100, new byte[100 * 100])).ToArray();
        return new StubWorldRegistry(maps);
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
