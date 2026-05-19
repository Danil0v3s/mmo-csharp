using Map.Server.Entities;

namespace Map.Server.Spawn;

/// <summary>
/// Owns the live mob population. Drives initial spawn on map load, idle
/// wander on the tick, and death + respawn scheduling.
/// </summary>
public interface IMobSpawnService
{
    /// <summary>
    /// Process every registered spawn entry: spawn missing mobs up to
    /// <see cref="MobSpawnEntry.Amount"/> per entry. Idempotent — safe to
    /// call repeatedly, only places mobs that don't already exist for the
    /// given entry. Skips entries for maps that aren't loaded.
    /// </summary>
    void SpawnInitial();

    /// <summary>
    /// Per-tick maintenance: idle mobs that have outlived
    /// <see cref="MobEntity.NextWanderTick"/> pick a new wander target and
    /// start walking; any pending respawns whose deadline has elapsed get
    /// instantiated.
    /// </summary>
    void Tick();

    /// <summary>
    /// Kill a live mob: broadcasts <c>ZC_NOTIFY_VANISH</c> (reason DEAD),
    /// removes it from the registry, and schedules respawn per the origin
    /// spawn entry. Returns true if a mob with this id existed.
    /// </summary>
    bool KillMob(EntityId id);

    /// <summary>
    /// Same as <see cref="KillMob(EntityId)"/> but attributes drops to
    /// <paramref name="lastHitter"/> (and their party) for the
    /// rAthena loot-protection windows. Pass null for environmental
    /// / GM kills where no player should get pickup priority.
    /// </summary>
    bool KillMob(EntityId id, PlayerEntity? lastHitter);

    /// <summary>Diagnostic: count of pending respawns waiting on the timer.</summary>
    int PendingRespawnCount { get; }

    /// <summary>
    /// rAthena <c>atcommand_monster</c> primitive — spawn one mob of
    /// <paramref name="classId"/> at (<paramref name="x"/>,<paramref name="y"/>)
    /// on <paramref name="mapName"/>. Returns the created entity or null
    /// if the map is unknown, mob_db doesn't have the class, or no
    /// walkable cell was free near the request point.
    /// </summary>
    Entity? SpawnAt(string mapName, int classId, short x, short y);
}
