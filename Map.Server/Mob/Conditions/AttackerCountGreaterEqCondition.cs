using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_ATTACKPCGE — fires when distinct PC attacker count ≥ cond2.</summary>
public sealed class AttackerCountGreaterEqCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.AttackerCountGreaterEq;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => mob.RudeAttackedCount >= entry.Cond2;
}
