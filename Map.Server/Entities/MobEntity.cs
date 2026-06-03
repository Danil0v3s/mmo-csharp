using System.Collections.Generic;
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
    public int ClassId { get; private set; }
    public string Name { get; private set; }
    public MobDbEntry? DbEntry { get; private set; }

    private IReadOnlyList<Map.Server.Status.BattleRace2>? _race2;
    /// <summary>COMBAT-81 — rAthena <c>status_get_race2</c>: the mob's race2 set (from mob_db
    /// <c>RaceGroups</c>), used by the <c>bAddRace2</c>/<c>bSubRace2</c> cardfix folds. Cached.</summary>
    public IReadOnlyList<Map.Server.Status.BattleRace2> Race2
        => _race2 ??= Map.Server.Status.Race2Map.FromRaceGroups(DbEntry?.RaceGroups);

    /// <summary>
    /// rAthena <c>mob_class_change</c> (mob.cpp ~4180) — swap this
    /// mob's identity in place. Class id / name / DbEntry change but
    /// the EntityId, position, and Hp ratio stay; the caller is
    /// responsible for re-broadcasting the appearance change.
    /// </summary>
    internal void SetClass(int newClassId, string newName, MobDbEntry? newDbEntry)
    {
        ClassId = newClassId;
        Name = newName;
        DbEntry = newDbEntry;
    }

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
    /// Wave 68 / Track D — rAthena <c>md-&gt;spawn_timer</c> origin
    /// tick (mob.cpp:1525). Stamped on registry insert; consumed by
    /// MSC_SPAWN to compare against a 1-tick "fresh spawn" window.
    /// Zero before the mob is registered.
    /// </summary>
    public long SpawnedAtTick { get; set; }

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

    /// <summary>
    /// rAthena <c>md-&gt;dmglog</c> — ring buffer of distinct attackers
    /// and their cumulative damage. Updated by
    /// <c>IDamageService.ApplyDamage</c>; queried by MSC_ATTACKPCGT /
    /// MSC_ATTACKPCGE for distinct-attacker count and by the EXP
    /// split path for share-of-damage.
    /// </summary>
    public Map.Server.Mob.MobDmgList DmgList { get; } = new();

    /// <summary>
    /// rAthena <c>md-&gt;ud.skill_id</c> — the skill id this mob most
    /// recently cast. Used by MSC_AFTERSKILL (mob_skill_db rows that
    /// chain to a specific previous skill). 0 = no recent cast.
    /// </summary>
    public ushort LastCastSkillId { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;trickcasting</c> (mob.hpp). Per-mob counter
    /// bumped by skills like NPC_TRICKDEAD that put the mob in a
    /// "fake-cast" state. MSC_TRICKCASTING fires while this is &gt; 0;
    /// MSC_ALCHEMIST fires only while it equals 0. Reset to 0 in
    /// <c>mob_spawn</c> (mob.cpp:1195) and on relevant SC ends.
    /// </summary>
    public int TrickCasting { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;spotted_log[DAMAGELOG_SIZE]</c> (mob.hpp:366).
    /// Set of char ids that have "seen" this mob (entered its view
    /// range). <c>mob_ai_sub_lazy</c> (mob.cpp:2418-2451) only rolls
    /// random-walk / idle-skill ticks when the log is non-empty; bosses
    /// further gate their AI on <c>battle_config.boss_active_time</c>.
    /// Reset by <c>mob_spawn</c> (mob.cpp:1200).
    /// </summary>
    public HashSet<int> SpottedLog { get; } = new();

    /// <summary>True if any char id is currently in <see cref="SpottedLog"/>.
    /// rAthena <c>mob_is_spotted</c> (mob.cpp:135).</summary>
    public bool IsSpotted => SpottedLog.Count > 0;

    /// <summary>
    /// rAthena <c>md-&gt;lootitems[LOOTITEM_SIZE]</c> + <c>lootitem_count</c>
    /// (mob.hpp). MD_LOOTER mobs (e.g. Hode, Sandman) pick up floor
    /// items into this bag during the AI tick; the contents are added
    /// back to the floor as drops when the mob dies (mob_dead). The
    /// per-mob cap is rAthena's <c>LOOTITEM_SIZE = 10</c>.
    /// </summary>
    public List<MobLootSlot> LootItems { get; } = new();

    /// <summary>True iff the mob has the <see cref="MobMode.Looter"/> bit
    /// set in its stats. Checked by <c>IMobLooterService</c>.</summary>
    public bool IsLooter => (Stats.Mode & MobMode.Looter) != 0;

    /// <summary>
    /// rAthena <c>md-&gt;state.steal_coin_flag</c> — set true the first
    /// time RG_STEALCOIN successfully mugs this mob; blocks further
    /// zeny steals from the same mob instance.
    /// </summary>
    public bool StolenCoin { get; set; }

    /// <summary>
    /// rAthena <c>md-&gt;special_state.ai</c> (map.hpp:436 <c>enum mob_ai</c>).
    /// Set when the mob is summoned (homunculus, alchemist sphere,
    /// Cannibalize, ABR, Bionic, etc.). Non-zero gates MSC_ALCHEMIST
    /// and the slave-stick-with-master variants. Defaults to
    /// <see cref="Mob.MobSpecialAi.None"/> for naturally spawned mobs.
    /// </summary>
    public Map.Server.Mob.MobSpecialAi SpecialAi { get; set; } = Map.Server.Mob.MobSpecialAi.None;

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
