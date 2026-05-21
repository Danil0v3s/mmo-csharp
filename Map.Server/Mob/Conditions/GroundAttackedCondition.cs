using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_GROUNDATTACKED — fires this tick if the mob took ground-unit damage.</summary>
public sealed class GroundAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.GroundAttacked;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.RecentGroundHit;
}
