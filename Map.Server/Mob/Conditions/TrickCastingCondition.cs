using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_TRICKCASTING (mob.cpp:4379-4380) —
/// <c>md-&gt;trickcasting &gt; 0</c>. The counter is bumped by skills
/// that put the mob in a fake-cast state (NPC_TRICKDEAD family) and
/// reset to 0 by <c>mob_spawn</c> (mob.cpp:1195).
///
/// <para>The counter lives on <see cref="MobEntity.TrickCasting"/>;
/// the actual increment/decrement landings happen in the SkillImpl
/// chain that owns NPC_TRICKDEAD (separate parity wave). This
/// evaluator just reads the current value.</para>
/// </summary>
public sealed class TrickCastingCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.TrickCasting;

    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => mob.TrickCasting > 0;
}
