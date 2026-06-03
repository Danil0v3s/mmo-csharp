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
    /// Client-facing IPv4 address this map server advertises to the char server
    /// after registering its map list. Char uses this when building
    /// HC_SEND_MAP_DATA so the client knows where to connect next. Default
    /// suits single-host development; production deployments override.
    /// </summary>
    public string MapIp { get; set; } = "127.0.0.1";

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
    /// One or more rAthena <c>map_cache.dat</c> files, searched in order at
    /// startup. Mirrors rAthena <c>map.cpp:3798-3802</c>: import → re/pre-re
    /// → root. The first cache containing a configured map name wins, so
    /// list narrower / per-mode caches first.
    /// </summary>
    public List<string> MapDataPaths { get; set; } = new();

    /// <summary>
    /// Whether to display the server version string to the client at login.
    /// Mirrors rAthena <c>battle_config.display_version</c>; when enabled
    /// the player sees a single self-message after spawn. Defaults to true
    /// since rAthena's default for display_version is 1.
    /// </summary>
    public bool DisplayVersion { get; set; } = true;

    /// <summary>
    /// Version string sent to the client when <see cref="DisplayVersion"/>
    /// is true. rAthena emits "Cannot determine SVN/Git version." when
    /// <c>get_git_hash()</c> fails — same default here so a fresh checkout
    /// matches the capture without extra config.
    /// </summary>
    public string VersionMessage { get; set; } = "Cannot determine SVN/Git version.";

    /// <summary>
    /// Lines emitted to the client after login (rAthena MOTD, read from
    /// <c>conf/motd.txt</c>). Inlined here for parity with the captured
    /// server. Empty lines and comment lines starting with <c>//</c> are
    /// skipped, mirroring rAthena's parser.
    /// </summary>
    public List<string> MotdLines { get; set; } = new()
    {
        "Welcome to rAthena! Enjoy! Please report any bugs you find."
    };

    /// <summary>
    /// FEATURE-15 — WoE (Agit) weekly schedule, in **server-local** time. Each entry is one
    /// recurring window; the <see cref="Map.Server.Agit.IWoeScheduler"/> auto-starts/ends the matching
    /// WoE edition at the window edges. Empty = no auto-WoE (GM/script start/end only).
    /// </summary>
    public List<WoeScheduleEntry> WoeSchedule { get; set; } = new();
}

/// <summary>
/// One recurring WoE window from <c>Server.WoeSchedule</c>. Strings so it binds cleanly from JSON;
/// parsed by <see cref="Map.Server.Agit.WoeWindow.TryParse"/>.
/// </summary>
public sealed class WoeScheduleEntry
{
    /// <summary>WoE edition: "1.0" / "FE", "2.0" / "SE", or "TE".</summary>
    public string Type { get; set; } = "1.0";

    /// <summary>Day of week the window opens, e.g. "Saturday".</summary>
    public string Day { get; set; } = "";

    /// <summary>Window open time-of-day, "HH:mm" (24h, server-local).</summary>
    public string Start { get; set; } = "";

    /// <summary>Window close time-of-day, "HH:mm". May be ≤ Start to cross midnight.</summary>
    public string End { get; set; } = "";
}