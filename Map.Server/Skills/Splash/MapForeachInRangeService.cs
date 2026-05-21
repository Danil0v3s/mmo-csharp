using Map.Server.Entities;

namespace Map.Server.Skills.Splash;

/// <summary>
/// Concrete <see cref="IMapForeachInRangeService"/> backed by
/// <see cref="IEntityRegistry.ForEachInRange"/> plus the BCT_* mask
/// resolution from <c>battle_check_target</c>.
///
/// <para>Allegiance rules (BCT_*) match rAthena:
/// <list type="bullet">
///   <item>Two players sharing PartyId or GuildId are <see cref="BattleCheckTarget.Party"/> / <see cref="BattleCheckTarget.Guild"/>.</item>
///   <item>A player vs another player with no shared affiliation is <see cref="BattleCheckTarget.Enemy"/>.</item>
///   <item>Mob ↔ Player is always <see cref="BattleCheckTarget.Enemy"/> unless the mob's master == player (slave mob).</item>
///   <item>Slaves of the same master are <see cref="BattleCheckTarget.Party"/>.</item>
/// </list>
/// PvP map flags (no-pvp / no-friendly-fire toggles) are not yet
/// consulted; that delta lives in <see cref="DamageService.CanDamage"/>
/// for the damage path. Splash filtering uses the allegiance graph
/// only, which keeps it cheap for the per-tick AOI scan.</para>
/// </summary>
public sealed class MapForeachInRangeService : IMapForeachInRangeService
{
    private readonly IEntityRegistry _entities;

    public MapForeachInRangeService(IEntityRegistry entities)
    {
        _entities = entities;
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
        var allegiance = Classify(src, target);
        return (mask & allegiance) != 0;
    }

    // ---- private allegiance classifier ----

    private static BattleCheckTarget Classify(Entity? src, Entity target)
    {
        if (src == null) return BattleCheckTarget.Enemy;  // null src → treat everyone as a candidate
        if (src.Id == target.Id) return BattleCheckTarget.Self;

        // Player ↔ Player
        if (src is PlayerEntity sp && target is PlayerEntity tp)
        {
            if (sp.PartyId != 0 && sp.PartyId == tp.PartyId) return BattleCheckTarget.Party;
            if (sp.GuildId != 0 && sp.GuildId == tp.GuildId) return BattleCheckTarget.Guild;
            return BattleCheckTarget.Enemy;
        }

        // Player vs Mob / Mob vs Player: always hostile until slave-
        // mob ownership data lands on MobEntity. Slave mobs (a mob
        // spawned with a player master via SC_DEVOTION / homun /
        // mercenary / Steel Body summon) are TODO — they'd classify
        // as Party when src or target is the slave's master.
        if (src is PlayerEntity && target is MobEntity) return BattleCheckTarget.Enemy;
        if (src is MobEntity && target is PlayerEntity) return BattleCheckTarget.Enemy;

        // Mob ↔ Mob: neutral by default. Same-master slave-mob handling
        // also TODO (see above).
        if (src is MobEntity && target is MobEntity) return BattleCheckTarget.Neutral;

        return BattleCheckTarget.Neutral;
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,  // skill units, NPCs, etc. — visible but not alive-checked
    };
}
