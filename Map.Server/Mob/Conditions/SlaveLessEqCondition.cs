using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SLAVELE — fires when slave count ≤ cond2.</summary>
public sealed class SlaveLessEqCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.SlaveLessEq;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        // See SlaveLessThanCondition note — currently a permissive stub.
        return entry.Cond2 >= 0;
    }
}
