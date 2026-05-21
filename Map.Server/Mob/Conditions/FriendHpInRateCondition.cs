using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_FRIENDHPINRATE — fires when at least one friend mob
/// within 8 tiles has Hp% in [cond2, val1].
/// </summary>
public sealed class FriendHpInRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.FriendHpInRate;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return false;
        return context.Slaves.GetFriendByHpRate(mob, entry.Cond2, entry.Val1) != null;
    }
}
