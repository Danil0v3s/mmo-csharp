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
/// firing this skill. First slice covers the bread-and-butter triggers
/// the AI ticker actually evaluates (HP %, always-fire, attack-state).
/// Full enum lives in mob.hpp; missing values plug in as they port.
/// </summary>
public enum MobSkillCondition : byte
{
    Always = 0,           // MSC_ALWAYS
    MyHpLessThanRate = 1, // MSC_MYHPLTMAXRATE
    SlaveLessThan = 2,    // MSC_SLAVELT — slave count below threshold
    AttackedBy = 3,       // MSC_ATTACKED — on receiving damage
    /// <summary>
    /// MSC_RUDEATTACKED — fires when the mob has been hit by an
    /// unreachable attacker more than <c>battle.mob_rudeattacked_count</c>
    /// times (rAthena default 2; mob.cpp:1748). Triggers <c>unit_escape</c>
    /// when no skill matches.
    /// </summary>
    RudeAttacked = 4,
}

/// <summary>
/// One mob_skill_db row — rAthena <c>struct s_mob_skill</c>. Trimmed to
/// what the C# port reads today; the rest of the columns are stored
/// opaquely on <see cref="MobDbEntry"/> until they're needed.
/// </summary>
public sealed record MobSkillEntry
{
    public required ushort SkillId { get; init; }
    public required ushort SkillLevel { get; init; }
    public required MobSkillState State { get; init; }
    public required MobSkillCondition Condition { get; init; }

    /// <summary>Permillage trigger rate — out of 10,000 (rAthena <c>permillage</c>).</summary>
    public int Permillage { get; init; } = 5_000; // default 50%

    /// <summary>Minimum delay between casts (ms).</summary>
    public int DelayMs { get; init; } = 5_000;

    /// <summary>Condition operand (e.g. HP%; meaning depends on <see cref="Condition"/>).</summary>
    public int Cond2 { get; init; }
}
