using Map.Server.Entities;

namespace Map.Server.Skills.Splash;

/// <summary>
/// Splash iteration helper — the C# counterpart of rAthena's
/// <c>map_foreachinrange(skill_area_sub, ..., src, ..., BCT_*, ...)</c>.
/// Every "for each enemy in N-tile splash" loop in <see cref="Map.Server.Skills.Behaviors"/>
/// routes through this so the BCT_* / friendly-fire / map-flag rules
/// are applied uniformly. Ports that left a <c>// Splash iteration TODO</c>
/// marker can replace it with one call to <see cref="ForEachEnemyInSplash"/>.
///
/// <para>The service is read-only over the entity registry; it never
/// mutates state on its own. Callers express their effect through the
/// <c>action</c> callback (apply damage, start SC, broadcast packet, …).</para>
/// </summary>
public interface IMapForeachInRangeService
{
    /// <summary>
    /// General-purpose form — iterate every entity within ±range of
    /// <paramref name="centerX"/>/<paramref name="centerY"/> whose
    /// allegiance to <paramref name="src"/> matches <paramref name="mask"/>,
    /// and invoke <paramref name="action"/> for each. <paramref name="src"/>
    /// supplies the allegiance reference; pass null to skip allegiance
    /// filtering entirely (returns every entity on the map in range).
    /// </summary>
    /// <returns>Number of times <paramref name="action"/> was invoked.</returns>
    int ForEachInSplash(
        Entity? src,
        uint mapId,
        short centerX,
        short centerY,
        short range,
        BattleCheckTarget mask,
        EntityType entityMask,
        System.Action<Entity> action);

    /// <summary>
    /// Convenience — <see cref="BattleCheckTarget.Enemy"/>, character
    /// entities (player + mob). Most damage splash skills use this.
    /// </summary>
    int ForEachEnemyInSplash(Entity src, short centerX, short centerY, short range, System.Action<Entity> action)
        => ForEachInSplash(src, src.MapId, centerX, centerY, range,
            BattleCheckTarget.Enemy, EntityType.Pc | EntityType.Mob, action);

    /// <summary>
    /// Convenience — <see cref="BattleCheckTarget.NoEnemy"/>, character
    /// entities. Used by Sanctuary-class friendly heal splashes.
    /// </summary>
    int ForEachAllyInSplash(Entity src, short centerX, short centerY, short range, System.Action<Entity> action)
        => ForEachInSplash(src, src.MapId, centerX, centerY, range,
            BattleCheckTarget.NoEnemy, EntityType.Pc | EntityType.Mob, action);

    /// <summary>
    /// Returns true iff <paramref name="target"/> matches <paramref name="mask"/>
    /// with respect to <paramref name="src"/>. Exposed for ports that
    /// already have the target in hand (single-target splash like
    /// chain-lightning's secondary jump) and just need the predicate.
    /// </summary>
    bool MatchesMask(Entity? src, Entity target, BattleCheckTarget mask);
}
