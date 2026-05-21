using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_FRIENDHPLTMAXRATE — fires when at least one friend mob
/// within 8 tiles has Hp% &lt; cond2. Real implementation requires
/// <see cref="MobConditionContext.Slaves"/> to be wired; falls
/// permissively to false when missing.
/// </summary>
public sealed class FriendHpLessThanRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.FriendHpLessThanRate;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return false;
        return context.Slaves.GetFriendByHpRate(mob, 0, entry.Cond2) != null;
    }
}
