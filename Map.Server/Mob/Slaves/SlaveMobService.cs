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
    // MOBAI-01 — walk primitive for the slave-follow pass. Null in tests that don't exercise follow.
    private readonly Map.Server.Movement.IMovementService? _movement;

    /// <summary>rAthena fixed friend search radius (mob.cpp:4124 / 4201).</summary>
    public const short FriendSearchRange = 8;

    // MOBAI-01 — rAthena slave-coupling constants (mob.hpp): keep the slave within 2 cells of its
    // master and throttle target inheritance to once per 300ms.
    private const int MobSlaveDistance = 2;   // MOB_SLAVEDISTANCE (mob.hpp:44)
    private const long MinMobLinkTime = 300;  // MIN_MOBLINKTIME (mob.hpp:36)

    public SlaveMobService(IEntityRegistry entities, IStatusChangeService? sc = null,
        Map.Server.Movement.IMovementService? movement = null)
    {
        _entities = entities;
        _sc = sc;
        _movement = movement;
    }

    /// <inheritdoc/>
    public SlaveTickResult TickSlave(MobEntity slave, long nowTick)
    {
        if (slave.MasterId is not { } masterId) return SlaveTickResult.Continue;
        var master = _entities.Get(masterId);

        // 1. Master dead or gone → the slave dies with it (rAthena status_kill(slave)).
        if (master is null || !Alive(master)) return SlaveTickResult.MasterGone;

        // Alive master on a different map: with slave_stick_with_master OFF (the rAthena default;
        // the unconditional warp arm is AI_ABR/AI_BIONIC-only) the slave cannot follow cross-map —
        // act independently this tick.
        if (master.MapId != slave.MapId) return SlaveTickResult.Continue;

        var dist = System.Math.Max(System.Math.Abs(master.X - slave.X), System.Math.Abs(master.Y - slave.Y));
        slave.MasterDist = dist;

        var canMove = (slave.Stats.Mode & MobMode.CanMove) != 0;
        if (canMove)
        {
            if (slave.TargetId != 0)
            {
                // Player-mastered slave that strayed > 5 cells from its player master: abandon the
                // target and walk back (rAthena BL_PC master → mob_unlocktarget + unit_walktobl).
                if (master is PlayerEntity && dist > 5)
                {
                    slave.TargetId = 0;
                    slave.AttackedId = 0;
                    _movement?.TryStartWalk(slave, master.X, master.Y);
                    return SlaveTickResult.Handled;
                }
                // Mob master, or close enough → keep fighting (rAthena returns 0).
                return SlaveTickResult.Continue;
            }

            // Approach the master when out of slave-distance (or standing on top of it).
            // (map_search_freecell keeps slaves off the master's exact cell in rAthena; the C#
            // movement layer resolves to the nearest walkable cell — see the MOBAI follow-up note.)
            if (dist > MobSlaveDistance || dist == 0)
            {
                _movement?.TryStartWalk(slave, master.X, master.Y);
                return SlaveTickResult.Handled;
            }
        }

        // 3. Target inheritance — only when idle, throttled to one per MIN_MOBLINKTIME (300ms).
        if (slave.TargetId == 0 && nowTick - slave.LastLinkTime >= MinMobLinkTime
            && ResolveMasterTarget(master) is { } tid
            && _entities.Get(tid) is PlayerEntity enemy && enemy.Hp > 0 && enemy.MapId == slave.MapId)
        {
            slave.TargetId = (int)tid.Value;
            slave.LastLinkTime = nowTick;
            return SlaveTickResult.Handled;
        }

        return SlaveTickResult.Continue;
    }

    /// <summary>MOBAI-01 — the master's current combat target id (engaged swing target, else the
    /// mob-master's locked/attacked target). Player masters carry it on <c>Attack.TargetId</c>.</summary>
    private static EntityId? ResolveMasterTarget(Entity master)
    {
        if (master.Attack is { } a && a.TargetId.Value != 0) return a.TargetId;
        if (master is MobEntity mm)
        {
            if (mm.TargetId != 0) return new EntityId(mm.TargetId);
            if (mm.AttackedId != 0) return new EntityId(mm.AttackedId);
        }
        return null;
    }

    private static bool Alive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };

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
