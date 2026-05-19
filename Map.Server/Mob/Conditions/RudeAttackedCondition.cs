using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena <c>MSC_RUDEATTACKED</c> evaluator — fires when the mob's
/// rude-attacked counter has crossed the configured threshold
/// (<c>battle.mob_rudeattacked_count</c>, default 2). The counter lives
/// on <see cref="MobAiService"/> and is reset when the mob lands a swing
/// (matches mob.cpp: rude-attacked clears on successful hit).
///
/// Evaluation here just inspects the live counter exposed through
/// <see cref="MobEntity.RudeAttackedCount"/>; incrementing is the
/// responsibility of <see cref="IMobAiService.NotifyAttacked"/>.
/// </summary>
public sealed class RudeAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.RudeAttacked;

    /// <summary>rAthena <c>battle.mob_rudeattacked_count</c> default.</summary>
    public const int DefaultThreshold = 2;

    public bool IsMet(MobEntity mob, Entity? target, MobSkillEntry entry)
    {
        return mob.RudeAttackedCount >= DefaultThreshold;
    }
}
