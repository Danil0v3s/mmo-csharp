using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_CLOSEDATTACKED — fires this tick if a melee hit landed.</summary>
public sealed class CloseAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.CloseAttacked;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => context.RecentMelee;
}
