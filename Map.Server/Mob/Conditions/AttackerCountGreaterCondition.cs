using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_ATTACKPCGT (mob.cpp:4359) — fires when the count of
/// distinct attackers (rAthena <c>unit_counttargeted</c>) &gt; cond2.
/// Backed by <see cref="MobEntity.DmgList"/> when populated, with a
/// RudeAttackedCount fallback for early-test scenarios where the
/// damage pipeline isn't wired.
/// </summary>
public sealed class AttackerCountGreaterCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.AttackerCountGreater;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        var count = mob.DmgList.DistinctAttackerCount;
        if (count == 0) count = mob.RudeAttackedCount;  // proxy when DmgList empty
        return count > entry.Cond2;
    }
}
