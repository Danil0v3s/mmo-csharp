using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_FRIENDSTATUSOFF — fires when at least one friend mob
/// within 8 tiles is missing SC <c>cond2</c>.
/// </summary>
public sealed class FriendStatusOffCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.FriendStatusOff;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (context.Slaves == null) return false;
        return context.Slaves.GetFriendByStatus(mob, Kind, (StatusType)entry.Cond2) != null;
    }
}
