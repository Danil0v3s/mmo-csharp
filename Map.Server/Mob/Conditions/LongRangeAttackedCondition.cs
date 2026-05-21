using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_LONGRANGEATTACKED — fires this tick if a ranged hit landed.</summary>
public sealed class LongRangeAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.LongRangeAttacked;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.RecentRanged;
}
