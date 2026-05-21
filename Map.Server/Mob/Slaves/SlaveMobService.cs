using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob.Slaves;

/// <summary>
/// Default <see cref="ISlaveMobService"/>. Walks the live
/// <see cref="IEntityRegistry"/> rather than maintaining a parallel
/// master→slaves index — keeps spawn/death paths free of bookkeeping,
/// at the cost of an O(N) scan per query. AI tick cadence is
/// 100ms+ per mob so the scan is amortised cheaply; if profiling
/// later shows it as a hot path we drop in a master-keyed dictionary.
/// </summary>
public sealed class SlaveMobService : ISlaveMobService
{
    private readonly IEntityRegistry _entities;
    private readonly IStatusChangeService? _sc;

    /// <summary>rAthena fixed friend search radius (mob.cpp:4124 / 4201).</summary>
    public const short FriendSearchRange = 8;

    public SlaveMobService(IEntityRegistry entities, IStatusChangeService? sc = null)
    {
        _entities = entities;
        _sc = sc;
    }

    /// <inheritdoc/>
    public int CountSlaves(Entity master)
    {
        var count = 0;
        foreach (var e in _entities.All())
        {
            if (e is not MobEntity m) continue;
            if (m.MapId != master.MapId) continue;
            if (m.Hp <= 0) continue;
            if (m.MasterId == master.Id) count++;
        }
        return count;
    }

    /// <inheritdoc/>
    public Entity? GetFriendByHpRate(MobEntity mob, int minRate, int maxRate)
    {
        // rAthena: friend search BL bucket = BL_MOB by default, BL_PC
        // when the mob is a summoned creature with a player master.
        // We detect "summoned by a player" by checking MasterId.Type.
        var lookForPlayers = false;
        if (mob.MasterId is { } masterId)
        {
            var master = _entities.Get(masterId);
            if (master is PlayerEntity) lookForPlayers = true;
        }

        var entityMask = lookForPlayers ? EntityType.Pc : EntityType.Mob;
        var candidates = _entities.ForEachInRange(mob.MapId, mob.X, mob.Y, FriendSearchRange, entityMask);
        foreach (var bl in candidates)
        {
            if (bl.Id == mob.Id) continue;             // skip self (rAthena mob.cpp:4099)
            if (!IsAlly(mob, bl)) continue;            // BCT_ENEMY filter (rAthena mob.cpp:4105)
            var (hp, max) = GetHp(bl);
            if (max <= 0) continue;
            var rate = hp * 100 / max;
            if (rate >= minRate && rate <= maxRate)
                return bl;
        }
        return null;
    }

    /// <inheritdoc/>
    public MobEntity? GetFriendByStatus(MobEntity mob, MobSkillCondition cond, StatusType type)
    {
        var candidates = _entities.ForEachInRange(mob.MapId, mob.X, mob.Y, FriendSearchRange, EntityType.Mob);
        foreach (var bl in candidates)
        {
            if (bl is not MobEntity friend) continue;
            if (friend.Id == mob.Id) continue;
            if (!IsAlly(mob, friend)) continue;

            var has = _sc?.Get(friend, type) != null;
            var wantOn = cond is MobSkillCondition.MyStatusOn or MobSkillCondition.FriendStatusOn;
            if (wantOn == has) return friend;
        }
        return null;
    }

    /// <inheritdoc/>
    public Entity? GetMasterIfHpBelow(MobEntity mob, int rate)
    {
        if (mob.MasterId is not { } masterId) return null;
        var master = _entities.Get(masterId);
        if (master == null) return null;
        var (hp, max) = GetHp(master);
        if (max <= 0) return null;
        if (hp * 100 / max < rate) return master;
        return null;
    }

    // --- helpers ---

    /// <summary>
    /// rAthena <c>battle_check_target(md, bl, BCT_ENEMY) &gt; 0</c> →
    /// returns true means "is enemy"; we invert to "is ally."
    /// Two mobs sharing the same master are allies; the mob itself
    /// is also an ally of its master.
    /// </summary>
    private static bool IsAlly(MobEntity mob, Entity other)
    {
        if (other.Id == mob.Id) return false;
        // Same master = ally (slave-of-slave or pet-of-pet).
        if (mob.MasterId is { } selfMaster
            && other is MobEntity om
            && om.MasterId == selfMaster) return true;
        // Other is the mob's master → ally.
        if (mob.MasterId is { } m && other.Id == m) return true;
        // Two wild mobs without masters are passively allied (rAthena
        // treats same-faction mobs as non-enemy by default).
        if (mob.MasterId == null && other is MobEntity om2 && om2.MasterId == null) return true;
        return false;
    }

    private static (int hp, int max) GetHp(Entity e) => e switch
    {
        PlayerEntity p => (p.Hp, p.MaxHp),
        MobEntity m => (m.Hp, m.MaxHp),
        _ => (0, 0),
    };
}
