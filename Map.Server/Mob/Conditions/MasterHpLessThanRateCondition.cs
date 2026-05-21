using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MASTERHPLTMAXRATE — fires when this mob's master has
/// Hp% &lt; cond2. Only applies to slave mobs (master_id != null).
/// </summary>
public sealed class MasterHpLessThanRateCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MasterHpLessThanRate;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return false;
        if (mob.MasterId == null) return false;
        return context.Slaves.GetMasterIfHpBelow(mob, entry.Cond2) != null;
    }
}
