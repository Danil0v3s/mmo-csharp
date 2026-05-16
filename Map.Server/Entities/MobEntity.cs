using Map.Server.Mob;
using Map.Server.Spawn;

namespace Map.Server.Entities;

/// <summary>
/// Live mob instance. <see cref="ClassId"/> + <see cref="DbEntry"/> point at
/// the static catalog ("what is a Poring"); the remaining fields are
/// per-instance runtime state ("this Poring at (152, 88), 30 HP left").
///
/// MS2 scope: enough state to spawn, wander, and die. Combat-side fields
/// (status changes, damage events, target tracking) come in MS3.
/// </summary>
public class MobEntity : Entity
{
    public int ClassId { get; }
    public string Name { get; }
    public MobDbEntry? DbEntry { get; }

    /// <summary>Spawn declaration that birthed this mob; used by respawn.</summary>
    public MobSpawnEntry? Origin { get; }

    /// <summary>Current HP. Set to <c>DbEntry.Hp</c> at spawn.</summary>
    public int Hp { get; set; }
    public int Sp { get; set; }

    /// <summary>
    /// Earliest tick (in <see cref="Environment.TickCount64"/> units) at which
    /// this mob is allowed to pick a new wander target. Walked mobs keep this
    /// in the future until they arrive.
    /// </summary>
    public long NextWanderTick { get; set; }

    public override EntityType Type => EntityType.Mob;

    public MobEntity(EntityId id, int classId, string name, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ClassId = classId;
        Name = name ?? string.Empty;
    }

    public MobEntity(EntityId id, MobDbEntry dbEntry, MobSpawnEntry origin, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ClassId = dbEntry.Id;
        DbEntry = dbEntry;
        Origin = origin;
        Name = string.IsNullOrEmpty(origin.DisplayName) ? dbEntry.Name : origin.DisplayName!;
        Hp = dbEntry.Hp;
        Sp = dbEntry.Sp;
        Speed = dbEntry.WalkSpeed > 0 ? dbEntry.WalkSpeed : Speed;
    }
}
