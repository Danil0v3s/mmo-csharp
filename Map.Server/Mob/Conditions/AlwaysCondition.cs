using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_ALWAYS — fires unconditionally on every tick.</summary>
public sealed class AlwaysCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.Always;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context) => true;
}
