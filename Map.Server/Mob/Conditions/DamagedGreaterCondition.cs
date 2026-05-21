using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_DAMAGEDGT — fires when cumulative damage taken &gt; cond2.</summary>
public sealed class DamagedGreaterCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.DamagedGreater;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.CumulativeDamageTaken > entry.Cond2;
}
