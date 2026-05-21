using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SLAVELT — fires when this mob's slave count &lt; cond2. Slave registry TODO.</summary>
public sealed class SlaveLessThanCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.SlaveLessThan;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        // Slave tracking lives in MobEntity.SlaveCount (TODO field).
        // Until it lands we treat the mob as having no slaves so any
        // cond2 > 0 trips.
        return entry.Cond2 > 0;
    }
}
