namespace Map.Server.Spawn;

/// <summary>
/// Read-only inventory of every mob spawn entry the map server is
/// responsible for. Today entries are populated programmatically (tests +
/// configuration); MS2's NPC parser will become the primary feeder once it
/// lands ([npc.md](../../../.agents/migrations/map/npc.md)).
/// </summary>
public interface IMobSpawnRegistry
{
    void Add(MobSpawnEntry entry);
    void AddRange(IEnumerable<MobSpawnEntry> entries);

    IReadOnlyList<MobSpawnEntry> GetForMap(uint mapId);
    IEnumerable<MobSpawnEntry> All();

    int Count { get; }
}
