using Map.Server.Status;

namespace Map.Server.Skills;

/// <summary>Skill targeting category — mirrors rAthena <c>e_inf</c>.</summary>
public enum SkillTargetMode : byte
{
    SelfOnly,         // INF_SELF_SKILL
    TargetEnemy,      // INF_ATTACK_SKILL
    TargetFriend,     // INF_SUPPORT_SKILL
    Ground,           // INF_GROUND_SKILL
    Passive,          // INF_PASSIVE_SKILL
}

/// <summary>Damage flavor — drives which calculator path resolves the skill.</summary>
public enum SkillDamageKind : byte
{
    None,        // no damage component (e.g. Heal, Blessing)
    Weapon,      // physical, uses BattleCalculator
    Magic,       // magical, uses MATK formula (skill of Magic Bolts)
    Misc,        // pre-computed damage / element fix only
    Heal,        // heals target HP
}

/// <summary>
/// Static catalog entry for one skill. rAthena <c>struct s_skill_db</c>
/// (skill.hpp), trimmed to the columns the gameplay path uses. A real
/// load from <c>skill_db.yml</c> / <c>skill_db</c> SQL table lands when
/// that subsystem ports — for the first slice the registry is hand-built.
/// </summary>
public sealed record SkillDefinition
{
    public required ushort Id { get; init; }
    public required string Name { get; init; }
    public required ushort MaxLevel { get; init; }
    public required SkillTargetMode Target { get; init; }
    public required SkillDamageKind DamageKind { get; init; }

    /// <summary>Cell range. 0 = self-only (no projectile / scan).</summary>
    public int Range { get; init; } = 1;

    /// <summary>SP cost per level (1-indexed: SpCost[1] = lvl 1 cost). Length should match MaxLevel+1.</summary>
    public int[] SpCost { get; init; } = Array.Empty<int>();

    /// <summary>Cast time (ms) per level. 0 = instant.</summary>
    public int[] CastTimeMs { get; init; } = Array.Empty<int>();

    /// <summary>Cooldown (ms) per level after resolution.</summary>
    public int[] CooldownMs { get; init; } = Array.Empty<int>();

    /// <summary>Damage formula coefficient per level (% of base damage).</summary>
    public int[] DamageRate { get; init; } = Array.Empty<int>();

    /// <summary>For Heal / status skills — secondary scaling per level.</summary>
    public int[] EffectAmount { get; init; } = Array.Empty<int>();

    /// <summary>Element of skill damage (used by Magic skills).</summary>
    public BattleElement Element { get; init; } = BattleElement.Neutral;

    /// <summary>SC applied by buff/debuff skills.</summary>
    public StatusType StatusType { get; init; } = StatusType.None;

    /// <summary>Duration (ms) per level for the applied SC. 0 = use a sensible default per skill.</summary>
    public int[] StatusDurationMs { get; init; } = Array.Empty<int>();
}
