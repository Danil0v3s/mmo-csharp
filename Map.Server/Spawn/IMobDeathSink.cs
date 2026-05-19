using Map.Server.Entities;

namespace Map.Server.Spawn;

/// <summary>
/// Narrow notification surface used by <c>DamageService</c> to drive a
/// mob death through the spawn/respawn pipeline without taking a hard
/// dependency on the full <see cref="IMobSpawnService"/>. The wider
/// service is implemented by <c>MobSpawnService</c>; the DI cycle
/// (mob spawn → movement → warp → setpos → attack → damage → mob spawn)
/// is broken by injecting this narrow seam into damage instead.
/// </summary>
public interface IMobDeathSink
{
    /// <summary>
    /// Mirrors <see cref="IMobSpawnService.KillMob(EntityId, PlayerEntity?)"/>:
    /// broadcast vanish, remove from registry, schedule respawn, attribute
    /// drops to <paramref name="lastHitter"/> (and party).
    /// </summary>
    bool KillMob(EntityId id, PlayerEntity? lastHitter);
}
