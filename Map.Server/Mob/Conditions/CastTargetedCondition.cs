using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_CASTTARGETED — fires when some entity is currently casting on this mob.</summary>
public sealed class CastTargetedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.CastTargeted;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.CastTargeted;
}
