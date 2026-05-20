using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// rAthena <c>battle_delay_damage</c> (battle.cpp:6320) — schedule
/// a damage application N ms in the future. Used for projectile
/// skills where the skill cast emits a packet immediately but the
/// damage lands when the visual arrives.
///
/// <para>rAthena <c>battle_damage_area</c> (battle.cpp:6710)
/// applies the same delayed damage to every entity in a square
/// radius — used by skills like Magnum Break and Tetra Vortex.</para>
/// </summary>
public interface IDelayedDamageService
{
    /// <summary>Schedule a single delayed-damage hit.</summary>
    void Schedule(Entity src, Entity target, long damage, int delayMs, ushort skillId);

    /// <summary>
    /// rAthena <c>battle_damage_area</c>: damage every entity in a
    /// <paramref name="radius"/>-square around (<paramref name="centerX"/>,
    /// <paramref name="centerY"/>) on <paramref name="mapId"/>.
    /// </summary>
    void DamageArea(Entity src, uint mapId, short centerX, short centerY, int radius, long damage, ushort skillId);

    /// <summary>Tick: apply pending delayed-damage entries whose deadline has elapsed.</summary>
    void Tick(long nowTick);
}
