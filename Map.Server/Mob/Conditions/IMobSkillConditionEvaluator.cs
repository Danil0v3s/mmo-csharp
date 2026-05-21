using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// Strategy interface for evaluating a <see cref="MobSkillCondition"/>.
/// One implementation per condition type — registered in
/// <see cref="MobSkillConditionRegistry"/>.
///
/// Same shape as <see cref="Skills.Resolvers.ISkillResolver"/> — adding
/// a new mob_skill_db condition (e.g. <c>MASTERATTACKED</c>) ships a
/// new evaluator class instead of a new switch arm.
/// </summary>
public interface IMobSkillConditionEvaluator
{
    MobSkillCondition Kind { get; }

    /// <summary>
    /// True iff <paramref name="entry"/>'s trigger condition holds for
    /// <paramref name="mob"/> right now. <paramref name="context"/>
    /// carries the ambient signals (recent attacker, last-damage type,
    /// rude-attack counter) so the evaluator can read transient state
    /// without re-scanning every entity every tick.
    /// </summary>
    bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context);
}

/// <summary>
/// Bag of transient signals the AI ticker feeds to evaluators. Mirrors
/// the per-tick locals in rAthena <c>mob_ai_sub_hard</c>: who hit us,
/// from what range, what counter is rolling. Default-initialized when
/// the AI fires "no-attack" think-ticks.
/// </summary>
public sealed record MobConditionContext
{
    /// <summary>Current think-tick (ms since boot). Used for rate-limit windows.</summary>
    public long Tick { get; init; }
    /// <summary>The mob's currently engaged target (null if idle).</summary>
    public Entity? Target { get; init; }
    /// <summary>Entity that hit the mob most recently this tick (null otherwise).</summary>
    public Entity? Attacker { get; init; }
    /// <summary>True iff the most recent hit was a melee attack.</summary>
    public bool RecentMelee { get; init; }
    /// <summary>True iff the most recent hit was a ranged attack.</summary>
    public bool RecentRanged { get; init; }
    /// <summary>True iff the mob just took ground-unit damage.</summary>
    public bool RecentGroundHit { get; init; }
    /// <summary>Skill id another entity just used in our range (0 = none).</summary>
    public ushort SkillUsedNearby { get; init; }
    /// <summary>True iff a cast is targeting the mob right now.</summary>
    public bool CastTargeted { get; init; }
    /// <summary>Cumulative damage taken (used by MSC_DAMAGEDGT).</summary>
    public int CumulativeDamageTaken { get; init; }

    public static MobConditionContext Empty { get; } = new();
}
