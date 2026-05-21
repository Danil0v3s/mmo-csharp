namespace Map.Server.Mob;

/// <summary>
/// rAthena <c>enum MobSkillState</c> (mob.hpp). Pinned values match the
/// rAthena indices so mob_skill_db rows seeded later resolve without a
/// remap.
/// </summary>
public enum MobSkillState : byte
{
    Any = 0,
    Idle = 1,
    Walk = 2,
    Loot = 3,
    Dead = 4,
    Berserk = 5,
    Angry = 6,
    Rush = 7,
    Follow = 8,
    AnyTarget = 9,
}

/// <summary>
/// rAthena <c>enum e_mob_skill_condition</c> — when the mob considers
/// firing this skill. Full table from <c>mob.hpp:455-481</c>; values
/// pinned to rAthena indices so mob_skill_db rows resolve without
/// remap. The condition operand sits in
/// <see cref="MobSkillEntry.Cond2"/> and its meaning depends on the
/// chosen condition.
/// </summary>
public enum MobSkillCondition : byte
{
    /// <summary>MSC_ALWAYS — every think-tick.</summary>
    Always = 0,
    /// <summary>MSC_MYHPLTMAXRATE — Hp% &lt; cond2.</summary>
    MyHpLessThanRate = 1,
    /// <summary>MSC_MYHPINRATE — cond2 ≤ Hp% ≤ val1.</summary>
    MyHpInRate = 2,
    /// <summary>MSC_FRIENDHPLTMAXRATE — any friend's Hp% &lt; cond2.</summary>
    FriendHpLessThanRate = 3,
    /// <summary>MSC_FRIENDHPINRATE — any friend's Hp% in range.</summary>
    FriendHpInRate = 4,
    /// <summary>MSC_MYSTATUSON — mob has SC cond2 active.</summary>
    MyStatusOn = 5,
    /// <summary>MSC_MYSTATUSOFF — mob does NOT have SC cond2 active.</summary>
    MyStatusOff = 6,
    /// <summary>MSC_FRIENDSTATUSON — any friend has SC cond2 active.</summary>
    FriendStatusOn = 7,
    /// <summary>MSC_FRIENDSTATUSOFF — any friend missing SC cond2.</summary>
    FriendStatusOff = 8,
    /// <summary>MSC_ATTACKPCGT — attacker count &gt; cond2.</summary>
    AttackerCountGreater = 9,
    /// <summary>MSC_ATTACKPCGE — attacker count ≥ cond2.</summary>
    AttackerCountGreaterEq = 10,
    /// <summary>MSC_SLAVELT — slave count &lt; cond2.</summary>
    SlaveLessThan = 11,
    /// <summary>MSC_SLAVELE — slave count ≤ cond2.</summary>
    SlaveLessEq = 12,
    /// <summary>MSC_CLOSEDATTACKED — hit by a melee attack this tick.</summary>
    CloseAttacked = 13,
    /// <summary>MSC_LONGRANGEATTACKED — hit by a ranged attack this tick.</summary>
    LongRangeAttacked = 14,
    /// <summary>MSC_AFTERSKILL — fires once after the mob's previous skill resolves.</summary>
    AfterSkill = 15,
    /// <summary>MSC_SKILLUSED — fires when *anyone* in range cast skill cond2.</summary>
    SkillUsed = 16,
    /// <summary>MSC_CASTTARGETED — fires when something is casting a skill on this mob.</summary>
    CastTargeted = 17,
    /// <summary>
    /// MSC_RUDEATTACKED — hit by an unreachable attacker more than
    /// <c>battle.mob_rudeattacked_count</c> times (default 2).
    /// Triggers <c>unit_escape</c> when no skill matches.
    /// </summary>
    RudeAttacked = 18,
    /// <summary>MSC_MASTERHPLTMAXRATE — slave: master Hp% &lt; cond2.</summary>
    MasterHpLessThanRate = 19,
    /// <summary>MSC_MASTERATTACKED — slave: master taking damage.</summary>
    MasterAttacked = 20,
    /// <summary>MSC_ALCHEMIST — Bioethics check for homunculus eligibility.</summary>
    Alchemist = 21,
    /// <summary>MSC_SPAWN — fires once on mob spawn.</summary>
    Spawn = 22,
    /// <summary>MSC_MOBNEARBYGT — same-class mob count nearby &gt; cond2.</summary>
    MobNearbyGreater = 23,
    /// <summary>MSC_GROUNDATTACKED — hit by a ground-placed unit.</summary>
    GroundAttacked = 24,
    /// <summary>MSC_DAMAGEDGT — total damage taken &gt; cond2.</summary>
    DamagedGreater = 25,
    /// <summary>MSC_TRICKCASTING — cast about to be interrupted.</summary>
    TrickCasting = 26,

    /// <summary>Legacy alias retained for older callers — equivalent to <see cref="CloseAttacked"/>.</summary>
    AttackedBy = CloseAttacked,
}

/// <summary>
/// rAthena <c>enum e_mob_skill_target</c> — which entity the chosen
/// skill targets. The picker resolves this at cast time, not when the
/// mob_skill_db row is loaded.
/// </summary>
public enum MobSkillTarget : byte
{
    /// <summary>MST_TARGET — the mob's current combat target.</summary>
    Target = 0,
    /// <summary>MST_RANDOM — a random hostile entity in range.</summary>
    Random = 1,
    /// <summary>MST_SELF — self-cast.</summary>
    Self = 2,
    /// <summary>MST_FRIEND — any friendly mob (party of master or same race).</summary>
    Friend = 3,
    /// <summary>MST_MASTER — the mob's master entity (slave/pet/homun).</summary>
    Master = 4,
    /// <summary>MST_AROUND5 — random cell within 5 tiles of mob.</summary>
    Around5 = 5,
    Around6 = 6,
    Around7 = 7,
    Around8 = 8,
    Around1 = 9,
    Around2 = 10,
    Around3 = 11,
    Around4 = 12,
}

/// <summary>
/// One mob_skill_db row — rAthena <c>struct s_mob_skill</c>. Mirrors
/// the columns in <c>db/(pre-)re/mob_skill_db.txt</c>:
/// (mob_id, info, state, skill_id, lv, rate, casttime, delay,
/// cancelable, target, condition, cond2, val1-val5, emotion, chat).
/// </summary>
public sealed record MobSkillEntry
{
    public required ushort SkillId { get; init; }
    public required ushort SkillLevel { get; init; }
    public required MobSkillState State { get; init; }
    public required MobSkillCondition Condition { get; init; }

    /// <summary>Permillage trigger rate — out of 10,000 (rAthena <c>permillage</c>).</summary>
    public int Permillage { get; init; } = 5_000; // default 50%

    /// <summary>Cast time in ms (rAthena column 7).</summary>
    public int CastTimeMs { get; init; } = 0;

    /// <summary>Minimum delay between casts in ms (rAthena column 8).</summary>
    public int DelayMs { get; init; } = 5_000;

    /// <summary>True iff cast can be interrupted by damage (rAthena column 9).</summary>
    public bool Cancelable { get; init; } = true;

    /// <summary>Target resolution mode (rAthena column 10).</summary>
    public MobSkillTarget Target { get; init; } = MobSkillTarget.Target;

    /// <summary>Condition operand (e.g. HP%; meaning depends on <see cref="Condition"/>).</summary>
    public int Cond2 { get; init; }

    /// <summary>Auxiliary args (rAthena val1-val5; meaning per-skill).</summary>
    public int Val1 { get; init; }
    public int Val2 { get; init; }
    public int Val3 { get; init; }
    public int Val4 { get; init; }
    public int Val5 { get; init; }

    /// <summary>Emotion index broadcast when this skill fires (-1 = none).</summary>
    public int Emotion { get; init; } = -1;

    /// <summary>Chat string ID broadcast when this skill fires (0 = none).</summary>
    public int ChatId { get; init; }
}
