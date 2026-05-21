using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SPAWN — fires once on mob spawn.</summary>
public sealed class SpawnCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.Spawn;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        // Spawn fires for the first ~500ms after the mob entered the
        // world. NextWanderTick is set on spawn to (now + walkdelay);
        // we use that as a proxy for "freshly spawned."
        return mob.NextWanderTick > context.Tick;
    }
}
