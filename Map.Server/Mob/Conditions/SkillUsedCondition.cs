using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SKILLUSED — fires when SkillUsedNearby == cond2 (a specific skill was just cast in range).</summary>
public sealed class SkillUsedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.SkillUsed;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.SkillUsedNearby != 0 && (int)context.SkillUsedNearby == entry.Cond2;
}
