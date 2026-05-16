using Core.Server;

namespace Map.Server;

public class MapServerConfiguration : ServerConfiguration
{
    public int MapLoadDistance { get; set; } = 2;

    /// <summary>
    /// Server id this map server registers with on the char server. Must be unique
    /// across all running map servers connecting to the same char server.
    /// </summary>
    public int ServerId { get; set; } = 1;

    /// <summary>
    /// List of map names this server is authoritative for (rAthena `chmapif_parse_getmapname`
    /// payload). Pushed to the char server on startup via RegisterMapServerMaps.
    /// </summary>
    public List<string> Maps { get; set; } = new();

    /// <summary>
    /// How often to push KeepAlive to the char server (seconds). rAthena maps push every 30s.
    /// </summary>
    public int KeepAliveInterval { get; set; } = 30;

    /// <summary>
    /// How often to push user-count snapshot to the char server (seconds). rAthena maps push every 10s.
    /// </summary>
    public int UserCountSyncInterval { get; set; } = 10;

    /// <summary>
    /// Periodic batch save of online character state (seconds). rAthena default autosave=300s.
    /// </summary>
    public int AutosaveInterval { get; set; } = 300;

    /// <summary>
    /// Filesystem path to rAthena's renewal mapcache.dat. Loaded once at startup;
    /// only the maps listed in <see cref="Maps"/> are kept in memory.
    /// </summary>
    public string MapDataPath { get; set; } = "/Volumes/1TB/Projetos/rathena/db/re/map_cache.dat";

    /// <summary>
    /// Path to rAthena's renewal <c>mob_db.yml</c>. Loaded once at startup.
    /// </summary>
    public string MobDbPath { get; set; } = "/Volumes/1TB/Projetos/rathena/db/re/mob_db.yml";

    /// <summary>
    /// Optional override file applied on top of <see cref="MobDbPath"/>
    /// (rAthena <c>mob_db2.yml</c>). Missing file is silently skipped.
    /// </summary>
    public string MobDbOverridePath { get; set; } = "/Volumes/1TB/Projetos/rathena/db/re/mob_db2.yml";
}