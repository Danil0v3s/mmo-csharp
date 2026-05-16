using System.Collections.Concurrent;

namespace Map.Server.Spawn;

public sealed class MobSpawnRegistry : IMobSpawnRegistry
{
    private readonly ConcurrentDictionary<uint, List<MobSpawnEntry>> _byMap = new();
    private readonly List<MobSpawnEntry> _all = new();
    private readonly object _gate = new();

    public int Count
    {
        get { lock (_gate) return _all.Count; }
    }

    public void Add(MobSpawnEntry entry)
    {
        lock (_gate)
        {
            _all.Add(entry);
            var bucket = _byMap.GetOrAdd(entry.MapId, _ => new List<MobSpawnEntry>());
            bucket.Add(entry);
        }
    }

    public void AddRange(IEnumerable<MobSpawnEntry> entries)
    {
        foreach (var entry in entries) Add(entry);
    }

    public IReadOnlyList<MobSpawnEntry> GetForMap(uint mapId)
    {
        if (!_byMap.TryGetValue(mapId, out var bucket)) return Array.Empty<MobSpawnEntry>();
        lock (_gate) return bucket.ToArray();
    }

    public IEnumerable<MobSpawnEntry> All()
    {
        lock (_gate) return _all.ToArray();
    }
}
