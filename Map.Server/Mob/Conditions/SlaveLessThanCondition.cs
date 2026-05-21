using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_SLAVELT (mob.cpp:4357) — fires when this mob's live
/// slave count &lt; cond2. Real count via
/// <see cref="MobConditionContext.Slaves"/>; falls permissively to
/// true when the service is missing.
/// </summary>
public sealed class SlaveLessThanCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.SlaveLessThan;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return entry.Cond2 > 0;
        return context.Slaves.CountSlaves(mob) < entry.Cond2;
    }
}
