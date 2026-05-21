using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_AFTERSKILL (mob.cpp:4365) — fires once after the mob's
/// previous cast was skill id <c>cond2</c>. Drives chain casts like
/// "use Heal, then Blessing" on cleric mobs. Reads
/// <see cref="MobEntity.LastCastSkillId"/>; the picker sets this
/// after a successful StartCast.
/// </summary>
public sealed class AfterSkillCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.AfterSkill;
    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => mob.LastCastSkillId != 0 && mob.LastCastSkillId == entry.Cond2;
}
