namespace Map.Server.World;

/// <summary>
/// Singleton catalog of loaded maps. Built once at map server startup from
/// the configured mapcache.dat + map list; immutable thereafter.
/// </summary>
public interface IMapWorldRegistry
{
    /// <summary>Returns the loaded map, or null if not hosted on this server.</summary>
    MapData? Get(string name);

    /// <summary>All maps loaded on this server.</summary>
    IEnumerable<MapData> All { get; }

    /// <summary>Total cells across every loaded map (for startup logging).</summary>
    int TotalCells { get; }

    /// <summary>True iff this server is authoritative for the named map.</summary>
    bool Contains(string name);
}
