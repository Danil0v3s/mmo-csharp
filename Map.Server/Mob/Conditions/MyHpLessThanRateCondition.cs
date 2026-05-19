using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_MYHPLTMAXRATE — fires when (hp * 100 / maxhp) ≤ cond2.</summary>
public sealed class MyHpLessThanRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MyHpLessThanRate;
    public bool IsMet(MobEntity mob, Entity? target, MobSkillEntry entry)
        => mob.MaxHp > 0 && (mob.Hp * 100 / mob.MaxHp) <= entry.Cond2;
}
