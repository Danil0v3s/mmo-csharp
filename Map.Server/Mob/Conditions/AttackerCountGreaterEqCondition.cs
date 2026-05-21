using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_ATTACKPCGE (mob.cpp:4363) — fires when the count of
/// distinct attackers ≥ cond2. Backed by
/// <see cref="MobEntity.DmgList"/> when populated.
/// </summary>
public sealed class AttackerCountGreaterEqCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.AttackerCountGreaterEq;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        var count = mob.DmgList.DistinctAttackerCount;
        if (count == 0) count = mob.RudeAttackedCount;
        return count >= entry.Cond2;
    }
}
