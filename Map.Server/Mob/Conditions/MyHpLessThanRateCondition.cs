using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_MYHPLTMAXRATE — fires when (Hp * 100 / MaxHp) ≤ cond2.</summary>
public sealed class MyHpLessThanRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MyHpLessThanRate;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => mob.MaxHp > 0 && (mob.Hp * 100 / mob.MaxHp) <= entry.Cond2;
}
