using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MYHPINRATE — fires when (Hp * 100 / MaxHp) is in
/// [cond2, val1]. Both bounds inclusive. Used by mobs that should
/// react during a specific health window (e.g. enraged-only between
/// 50% and 25% HP).
/// </summary>
public sealed class MyHpInRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MyHpInRate;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (mob.MaxHp <= 0) return false;
        var pct = mob.Hp * 100 / mob.MaxHp;
        return pct >= entry.Cond2 && pct <= entry.Val1;
    }
}
