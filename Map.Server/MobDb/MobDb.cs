using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobDb"/>. Reads <c>mob_db.yml</c> at construction and
/// applies the optional <c>mob_db2.yml</c> override file on top, matching
/// rAthena's two-file loader. Lookup tables are rebuilt under a lock on
/// <see cref="Reload"/>; gameplay reads are lock-free against the snapshot.
/// </summary>
public sealed class MobDb : IMobDb
{
    private readonly string _primaryPath;
    private readonly string? _overridePath;
    private readonly ILogger<MobDb> _logger;
    private volatile Snapshot _snapshot;

    public MobDb(string primaryPath, string? overridePath, ILogger<MobDb> logger)
    {
        _primaryPath = primaryPath;
        _overridePath = overridePath;
        _logger = logger;
        _snapshot = LoadSnapshot();
    }

    public int Count => _snapshot.ById.Count;

    public MobDbEntry? Get(int classId) =>
        _snapshot.ById.TryGetValue(classId, out var e) ? e : null;

    public MobDbEntry? GetByAegisName(string aegisName) =>
        aegisName is null ? null :
        _snapshot.ByName.TryGetValue(aegisName, out var e) ? e : null;

    public IEnumerable<MobDbEntry> All() => _snapshot.ById.Values;

    public void Reload() => _snapshot = LoadSnapshot();

    private Snapshot LoadSnapshot()
    {
        var byId = new Dictionary<int, MobDbEntry>();
        Apply(_primaryPath, byId, required: true);
        if (!string.IsNullOrEmpty(_overridePath))
        {
            Apply(_overridePath, byId, required: false);
        }

        var byName = new Dictionary<string, MobDbEntry>(byId.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in byId.Values)
        {
            byName[entry.AegisName] = entry;
        }
        _logger.LogInformation("MobDb loaded {Count} entries", byId.Count);
        return new Snapshot(byId, byName);
    }

    private void Apply(string path, Dictionary<int, MobDbEntry> byId, bool required)
    {
        if (!File.Exists(path))
        {
            if (required)
            {
                throw new FileNotFoundException($"mob_db file not found: {path}", path);
            }
            _logger.LogInformation("MobDb override file {Path} not present, skipping", path);
            return;
        }

        using var reader = new StreamReader(path);
        var entries = MobDbYamlReader.Read(reader);
        foreach (var entry in entries)
        {
            byId[entry.Id] = entry;
        }
    }

    private sealed record Snapshot(
        IReadOnlyDictionary<int, MobDbEntry> ById,
        IReadOnlyDictionary<string, MobDbEntry> ByName);
}
