using Map.Server.World;

namespace Map.Server.Session;

/// <summary>
/// Decides which loaded map an authenticated character should spawn on.
/// Extracted from <c>WantToConnectionHandler</c> so the resolution rules
/// can be unit-tested without standing up the full handler + gRPC IPC
/// service.
/// </summary>
public static class SpawnMapResolver
{
    /// <summary>
    /// Resolution order:
    /// <list type="number">
    ///   <item>Character's last logout map, if loaded on this server.</item>
    ///   <item>First map in <paramref name="configuredMaps"/> that's loaded.</item>
    ///   <item>Any loaded map (defensive fallback).</item>
    /// </list>
    /// <paramref name="savedMapName"/> tolerates an optional <c>.gat</c>
    /// suffix; rAthena stores map names without the extension but some
    /// IPC paths add it.
    /// </summary>
    public static MapData? Resolve(
        IMapWorldRegistry worldRegistry,
        IEnumerable<string> configuredMaps,
        string? savedMapName)
    {
        if (!string.IsNullOrEmpty(savedMapName))
        {
            var trimmed = savedMapName.EndsWith(".gat", StringComparison.OrdinalIgnoreCase)
                ? savedMapName[..^4]
                : savedMapName;
            var saved = worldRegistry.Get(trimmed);
            if (saved != null) return saved;
        }

        foreach (var name in configuredMaps)
        {
            var map = worldRegistry.Get(name);
            if (map != null) return map;
        }
        return worldRegistry.All.FirstOrDefault();
    }
}
