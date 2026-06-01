using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Skills.Splash;

/// <summary>
/// Concrete <see cref="IMapForeachInRangeService"/> backed by
/// <see cref="IEntityRegistry.ForEachInRange"/> plus the BCT_* mask
/// resolution from <c>battle_check_target</c> (via the shared
/// <see cref="BattleTargetResolver"/>).
///
/// <para>SKILL-03: allegiance now honors summoned-slave master substitution
/// (a player's slave is friendly to the player + party) and the PvP/GvG/BG
/// mapflags (unaffiliated players are only mutually attackable in a hostile
/// zone; <c>pvp_noparty</c>/<c>gvg_noparty</c>/<c>pvp_noguild</c> re-enable
/// friendly fire). The same resolver backs <c>DamageService.CanDamage</c> so
/// the splash victim filter and the damage gate cannot disagree.</para>
/// </summary>
public sealed class MapForeachInRangeService : IMapForeachInRangeService
{
    private readonly IEntityRegistry _entities;
    private readonly IMapFlagService? _mapFlags;
    private readonly IMapWorldRegistry? _world;

    public MapForeachInRangeService(
        IEntityRegistry entities,
        IMapFlagService? mapFlags = null,
        IMapWorldRegistry? world = null)
    {
        _entities = entities;
        _mapFlags = mapFlags;
        _world = world;
    }

    public int ForEachInSplash(
        Entity? src,
        uint mapId,
        short centerX,
        short centerY,
        short range,
        BattleCheckTarget mask,
        EntityType entityMask,
        System.Action<Entity> action)
    {
        if (mask == BattleCheckTarget.None || range < 0) return 0;
        var hits = _entities.ForEachInRange(mapId, centerX, centerY, range, entityMask);
        var count = 0;
        foreach (var e in hits)
        {
            if (!IsAlive(e)) continue;
            if (!MatchesMask(src, e, mask)) continue;
            action(e);
            count++;
        }
        return count;
    }

    public bool MatchesMask(Entity? src, Entity target, BattleCheckTarget mask)
    {
        var allegiance = BattleTargetResolver.Classify(src, target, _entities, _mapFlags, _world);
        return (mask & allegiance) != 0;
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,  // skill units, NPCs, etc. — visible but not alive-checked
    };
}
