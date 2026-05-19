using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

public sealed class AlwaysCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.Always;
    public bool IsMet(MobEntity mob, Entity? target, MobSkillEntry entry) => true;
}
