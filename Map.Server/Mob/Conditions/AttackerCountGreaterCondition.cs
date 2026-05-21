using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_ATTACKPCGT — fires when distinct PC attacker count &gt; cond2. Attacker bookkeeping TODO.</summary>
public sealed class AttackerCountGreaterCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.AttackerCountGreater;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        // Until DmgListLog porting lands we estimate from RudeAttackedCount
        // as a proxy for "is being attacked." Not parity-true but harmless.
        return mob.RudeAttackedCount > entry.Cond2;
    }
}
