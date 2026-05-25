using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>rAthena MSC_SPAWN — fires once on mob spawn.</summary>
public sealed class SpawnCondition : IMobSkillConditionEvaluator
{
    /// <summary>
    /// Spawn window. rAthena fires MSC_SPAWN immediately after
    /// <c>md->spawn_timer</c> is set; the AI ticker visits the mob
    /// for the first time within ~500 ms. Matching that with a 1 s
    /// window gives a safe inclusive band.
    /// </summary>
    private const long SpawnWindowMs = 1000;

    public MobSkillCondition Kind => MobSkillCondition.Spawn;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        // Wave 68 / Track D — precise spawn-tick window. SpawnedAtTick
        // is stamped by MobSpawnService on registry insert (mob.cpp:1525
        // `md->spawn_timer`). Fires only inside the 1 s window after
        // first registration. Falls back to the legacy NextWanderTick
        // proxy when SpawnedAtTick is unset (mobs created before the
        // field landed, or by alternate spawn paths).
        if (mob.SpawnedAtTick > 0)
            return context.Tick - mob.SpawnedAtTick < SpawnWindowMs;
        return mob.NextWanderTick > context.Tick;
    }
}
