using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SLAVELE — fires when slave count ≤ cond2.</summary>
public sealed class SlaveLessEqCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.SlaveLessEq;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return entry.Cond2 >= 0;
        return context.Slaves.CountSlaves(mob) <= entry.Cond2;
    }
}
