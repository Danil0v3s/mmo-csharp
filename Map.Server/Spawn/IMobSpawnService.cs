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

    /// <summary>
    /// rAthena <c>mob_once_spawn_sub</c> with the special-AI tag set —
    /// summon a mob bound to <paramref name="masterId"/>, marked as one
    /// of the script-summoned variants (Alchemist Bionic, Sphere,
    /// Flora, Geneticist Plant, Mechanic FAW turret, Sorcerer ABR pet,
    /// Ninja Zanzou clone). The lifetime parameter caps the entity at
    /// <paramref name="lifetimeMs"/> ms (0 = no expiry); naturally dies
    /// when MobEntity.Hp ≤ 0 or the timer fires.
    ///
    /// <para>Differs from <see cref="SpawnAt"/> in three ways: (1) sets
    /// <see cref="Entity.MasterId"/> so MasterAttackedCondition routes
    /// aggro through the owner; (2) sets
    /// <see cref="MobEntity.SpecialAi"/> so the slave-AI branches fire
    /// (e.g. AI_SPHERE triggers Cannibalize chase); (3) records the
    /// expiry in the despawn queue so the summon vanishes on time.</para>
    /// </summary>
    Entity? SpawnWithAi(EntityId masterId, uint mapId, int classId, short x, short y,
        Mob.MobSpecialAi aiTag, int lifetimeMs = 0);
}
