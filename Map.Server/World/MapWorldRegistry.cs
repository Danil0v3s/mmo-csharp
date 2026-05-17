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
    /// Load the maps the server is configured to host. Walks the cache file
    /// list in order — for each configured map name, the first cache that
    /// has it wins; subsequent caches are only consulted if earlier ones
    /// don't contain that map. Mirrors rAthena's per-map fallback through
    /// import → re/pre-re → root caches (map.cpp:3845-3848).
    ///
    /// Logs a warning + skips any map that no cache contains.
    /// </summary>
    public static MapWorldRegistry Load(
        IReadOnlyList<string> mapCachePaths,
        IEnumerable<string> mapNames,
        ILogger? logger = null)
    {
        if (mapCachePaths.Count == 0)
        {
            throw new ArgumentException(
                "No map cache paths configured; set Server.MapDataPaths in appsettings.json",
                nameof(mapCachePaths));
        }

        var reader = new MapCacheReader();
        var caches = new List<(string Path, IReadOnlyDictionary<string, MapData> Maps)>(mapCachePaths.Count);
        foreach (var path in mapCachePaths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"mapcache.dat not found at '{path}'", path);
            }
            caches.Add((path, reader.ReadAll(path, logger)));
        }

        var loaded = new List<MapData>();
        foreach (var name in mapNames)
        {
            MapData? hit = null;
            string? hitPath = null;
            foreach (var (path, maps) in caches)
            {
                if (maps.TryGetValue(name, out var map))
                {
                    hit = map;
                    hitPath = path;
                    break;
                }
            }
            if (hit != null)
            {
                loaded.Add(hit);
                logger?.LogInformation(
                    "Loaded map '{Name}' from {Path}", name, hitPath);
            }
            else
            {
                logger?.LogWarning(
                    "Map '{Name}' configured but not present in any of: {Paths}",
                    name, string.Join(", ", mapCachePaths));
            }
        }

        var registry = new MapWorldRegistry(loaded);
        logger?.LogInformation(
            "World loaded: {Count} maps, {Cells} total cells (from {CachesCount} cache file(s))",
            loaded.Count, registry.TotalCells, mapCachePaths.Count);
        return registry;
    }
}
