using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Entities;

/// <summary>
/// Live mob instance. <see cref="ClassId"/> + <see cref="DbEntry"/> point at
/// the static catalog ("what is a Poring"); the remaining fields are
/// per-instance runtime state ("this Poring at (152, 88), 30 HP left").
///
/// MS2 scope: enough state to spawn, wander, and die. Combat-side fields
/// (status changes, damage events, target tracking) come in MS3.
/// </summary>
public class MobEntity : Entity
{
    public int ClassId { get; }
    public string Name { get; }
    public MobDbEntry? DbEntry { get; }

    /// <summary>Spawn declaration that birthed this mob; used by respawn.</summary>
    public MobSpawnEntry? Origin { get; }

    /// <summary>Current HP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Hp
    {
        get => Stats.Hp;
        set => Stats.Hp = value;
    }

    /// <summary>Maximum HP. Backed by <see cref="Entity.Stats"/>; mob_db copies in at spawn via the calc service.</summary>
    public int MaxHp
    {
        get => Stats.MaxHp;
        internal set => Stats.MaxHp = value;
    }

    /// <summary>Current SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Sp
    {
        get => Stats.Sp;
        set => Stats.Sp = value;
    }

    /// <summary>
    /// Earliest tick (in <see cref="Environment.TickCount64"/> units) at which
    /// this mob is allowed to pick a new wander target. Walked mobs keep this
    /// in the future until they arrive.
    /// </summary>
    public long NextWanderTick { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;state.attacked_count</c> (mob.cpp:1748). Incremented
    /// every time the mob takes damage from an attacker it can't reach
    /// (out of range / pathing-blocked). Once it crosses
    /// <c>battle.mob_rudeattacked_count</c> the AI fires MSC_RUDEATTACKED
    /// (or escapes if no skill matches). Reset on each successful melee
    /// swing.
    /// </summary>
    public int RudeAttackedCount { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;state.skillstate</c> — the FSM bucket the AI is in
    /// right now (Berserk / Idle / Walk / Loot / Dead / Angry / Rush /
    /// Follow / AnyTarget). Drives mob_skill_db row filtering: a row's
    /// State must equal this (or be Any / AnyTarget) for the picker to
    /// consider it. Default Idle on spawn.
    /// </summary>
    public MobSkillState SkillState { get; set; } = MobSkillState.Idle;

    // NOTE: master_id (rAthena md->master_id) lives on the base
    // <see cref="Entity.MasterId"/> (EntityId?) — used by the slave-AI
    // follow loop, MST_MASTER target resolver, and
    // MSC_MASTERHPLTMAXRATE / MSC_MASTERATTACKED conditions.

    /// <summary>
    /// rAthena <c>md-&gt;target_id</c>. Mirrors the engaged combat target's
    /// EntityId so MST_TARGET can resolve without re-scanning the AOI.
    /// 0 = no current target. Set by mob_target / mob_unlocktarget.
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;attacked_id</c> — the last entity that hit this mob,
    /// even if it's not currently <see cref="TargetId"/>. Used as a fallback
    /// for MST_TARGET when the primary target is null and MD_CANATTACK is
    /// not set on the mob.
    /// </summary>
    public int AttackedId { get; set; }

    public override EntityType Type => EntityType.Mob;

    public MobEntity(EntityId id, int classId, string name, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ClassId = classId;
        Name = name ?? string.Empty;
    }

    public MobEntity(EntityId id, MobDbEntry dbEntry, MobSpawnEntry origin, uint mapId, short x, short y)
        : base(id, mapId, x, y)
    {
        ClassId = dbEntry.Id;
        DbEntry = dbEntry;
        Origin = origin;
        Name = string.IsNullOrEmpty(origin.DisplayName) ? dbEntry.Name : origin.DisplayName!;
        Hp = dbEntry.Hp;
        MaxHp = dbEntry.Hp;
        Sp = dbEntry.Sp;
        Speed = dbEntry.WalkSpeed > 0 ? dbEntry.WalkSpeed : Speed;
        Level = dbEntry.Level;
        // IStatusCalcService.CalcMob() at spawn fills the rest from DbEntry;
        // these defaults keep parity-test code working before that wires in.
    }
}
