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
    /// <summary>
    /// Optional slave / friend / master lookup service. When non-null,
    /// the friend-/slave-/master-class evaluators read through this;
    /// when null they fall back to the conservative stubs.
    /// </summary>
    public Slaves.ISlaveMobService? Slaves { get; init; }

    /// <summary>
    /// Optional status-change service for MSC_MYSTATUSON /
    /// MSC_MYSTATUSOFF self-status reads. Friend-status variants ride
    /// on <see cref="Slaves.ISlaveMobService.GetFriendByStatus"/>;
    /// only the self-status branch needs this directly.
    /// </summary>
    public Map.Server.Status.IStatusChangeService? Sc { get; init; }

    /// <summary>
    /// Optional entity registry for MSC_MOBNEARBYGT (range scan of
    /// same-class mobs). Null is acceptable — the evaluator falls
    /// back to "false" rather than throwing.
    /// </summary>
    public Map.Server.Entities.IEntityRegistry? Entities { get; init; }

    /// <summary>
    /// Distinct PC-attacker count (rAthena <c>unit_counttargeted</c>).
    /// Used by MSC_ATTACKPCGT / MSC_ATTACKPCGE. Zero when the
    /// DmgListLog hasn't been queried yet.
    /// </summary>
    public int DistinctAttackerCount { get; init; }

    /// <summary>
    /// The skill id this mob cast on its previous tick (rAthena
    /// <c>md-&gt;ud.skill_id</c>). Used by MSC_AFTERSKILL.
    /// </summary>
    public ushort LastCastSkillId { get; init; }

    public static MobConditionContext Empty { get; } = new();
}
