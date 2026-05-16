namespace Map.Server.World;

public sealed class MapWorldRegistry : IMapWorldRegistry
{
    private readonly Dictionary<string, MapData> _byName;

    public MapWorldRegistry(IEnumerable<MapData> maps)
    {
        _byName = new Dictionary<string, MapData>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in maps)
        {
            _byName[map.Name] = map;
        }
    }

    public MapData? Get(string name) => _byName.GetValueOrDefault(name);

    public IEnumerable<MapData> All => _byName.Values;

    public int TotalCells => _byName.Values.Sum(m => m.CellCount);

    public bool Contains(string name) => _byName.ContainsKey(name);

    /// <summary>
    /// Load the maps the server is configured to host from mapcache.dat.
    /// Logs a warning + skips any map not present in the cache. Returns a
    /// ready-to-use registry.
    /// </summary>
    public static MapWorldRegistry Load(
        string mapCachePath,
        IEnumerable<string> mapNames,
        ILogger? logger = null)
    {
        if (!File.Exists(mapCachePath))
        {
            throw new FileNotFoundException(
                $"mapcache.dat not found at '{mapCachePath}'", mapCachePath);
        }

        var reader = new MapCacheReader();
        var all = reader.ReadAll(mapCachePath);
        var loaded = new List<MapData>();

        foreach (var name in mapNames)
        {
            if (all.TryGetValue(name, out var map))
            {
                loaded.Add(map);
            }
            else
            {
                logger?.LogWarning(
                    "Map '{Name}' configured but not present in {Path}",
                    name, mapCachePath);
            }
        }

        var registry = new MapWorldRegistry(loaded);
        logger?.LogInformation(
            "World loaded: {Count} maps, {Cells} total cells (from {Path})",
            loaded.Count, registry.TotalCells, mapCachePath);
        return registry;
    }
}
