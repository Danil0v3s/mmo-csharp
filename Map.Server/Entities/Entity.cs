using Map.Server.Movement;
using Map.Server.Status;

namespace Map.Server.Entities;

/// <summary>
/// Abstract base for every entity that lives on a map. Mirrors rAthena's
/// <c>struct block_list</c> (map.hpp). The fields here are the hot path for
/// the spatial index and visibility checks; subclasses add per-type state.
///
/// Coordinates are cell-based <see cref="short"/> values matching the wire
/// protocol; direction is the rAthena 8-direction enum (0=N, 1=NW, 2=W, 3=SW,
/// 4=S, 5=SE, 6=E, 7=NE; see <see cref="Movement.Direction"/>).
/// </summary>
public abstract class Entity
{
    public EntityId Id { get; }
    public abstract EntityType Type { get; }
    public uint MapId { get; internal set; }
    public short X { get; internal set; }
    public short Y { get; internal set; }
    public byte Dir { get; internal set; }

    /// <summary>
    /// Non-null while this entity is walking. Owned by <c>IMovementService</c>;
    /// gameplay code reads but does not mutate.
    /// </summary>
    public WalkState? Walk { get; internal set; }

    /// <summary>
    /// Non-null while this entity is auto-attacking. Owned by
    /// <see cref="Combat.IAttackService"/>; mirrors the subset of rAthena
    /// <c>unit_data</c> the attack-loop reads.
    /// </summary>
    public Combat.AttackState? Attack { get; internal set; }

    /// <summary>
    /// Wave 69 / Track B — rAthena <c>ud.canmove_tick</c> (unit.cpp:1450).
    /// Earliest <c>Environment.TickCount64</c> at which this entity is
    /// allowed to start a new walk. Pushed forward by
    /// <c>IMovementService.SetWalkDelay</c> on hit-stun; honored by
    /// <c>TryStartWalk</c>. Zero when no freeze is active.
    /// </summary>
    public long WalkableAfterTick { get; set; }

    /// <summary>
    /// Wave 71 / Track D — rAthena <c>unit_counttargeted</c> (unit.cpp:1834).
    /// Count of distinct entities currently latched onto this one as
    /// their attack target. Incremented by
    /// <c>IAttackService.StartAttack</c> when a fresh latch lands;
    /// decremented by <c>StopAttack</c> and on death. Read by mob AI
    /// (MSC_ATTACKPCGT condition) and the rude-attacked steal logic.
    /// Mirrors the inverse map rAthena builds by walking the registry
    /// — the C# port keeps the count on the target instead of scanning
    /// every entity on every read.
    /// </summary>
    public int Targeters { get; set; }

    /// <summary>
    /// Owner / master EntityId for summons (pets, homunculi, mercenaries,
    /// elementals, slave mobs). Null for free-roaming mobs and PCs.
    /// Read by <see cref="Mob.ISummonAiService"/>; set at summon spawn.
    /// </summary>
    public EntityId? MasterId { get; internal set; }

    /// <summary>
    /// Per-cell step delay in milliseconds (cardinal). Diagonal cells cost
    /// <c>Speed * 14 / 10</c>. PlayerEntity default = 150 (rAthena pc->speed
    /// baseline); mobs override from mob_db.MoveSpeed in MS2.
    /// </summary>
    public int Speed { get; internal set; } = 150;

    /// <summary>
    /// Renewal battle status block. Populated by <c>IStatusCalcService</c>
    /// at entity spawn / equip / level-up / SC apply. Always non-null —
    /// mirror of rAthena <c>bl-&gt;status_data</c> (status.hpp:3328) which
    /// is also always present, just zeroed for non-combat entities.
    /// </summary>
    public BattleStats Stats { get; } = new();

    /// <summary>Renewal base level (status_calc_misc input).</summary>
    public int Level { get; internal set; } = 1;

    protected Entity(EntityId id, uint mapId, short x, short y)
    {
        Id = id;
        MapId = mapId;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Update the entity's cell position. Callers must also notify the
    /// spatial index (via <c>IEntityRegistry.Move</c>) for the bucket
    /// rebinding to happen.
    /// </summary>
    internal void SetPosition(uint mapId, short x, short y)
    {
        MapId = mapId;
        X = x;
        Y = y;
    }
}
