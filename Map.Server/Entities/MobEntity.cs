namespace Map.Server.Entities;

/// <summary>
/// Stub for MS2 — see <c>.agents/migrations/map/mob-db.md</c> and
/// <c>.agents/migrations/map/spawn.md</c>. Mobs are live monster instances
/// spawned per the map's mob config; populated with stats from MobDbEntry.
/// </summary>
public class MobEntity : Entity
{
    public int ClassId { get; }
    public string Name { get; }

    public override EntityType Type => EntityType.Mob;

    public MobEntity(EntityId id, int classId, string name, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ClassId = classId;
        Name = name ?? string.Empty;
    }
}
