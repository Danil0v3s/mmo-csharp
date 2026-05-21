using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MYSTATUSOFF (mob.cpp:4341) — inverse of MSC_MYSTATUSON.
/// Fires when the mob is missing SC <c>cond2</c>. Same SC_NONE special
/// case (cond2 == 0 → fires if the mob has no common SC at all).
/// </summary>
public sealed class MyStatusOffCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MyStatusOff;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Sc == null) return false;
        if (entry.Cond2 == 0)
        {
            // rAthena: !found across the common-SC block.
            for (int i = (int)StatusType.Stone; i <= (int)StatusType.Bleeding; i++)
                if (context.Sc.Get(mob, (StatusType)i) != null) return false;
            return true;
        }
        return context.Sc.Get(mob, (StatusType)entry.Cond2) == null;
    }
}
