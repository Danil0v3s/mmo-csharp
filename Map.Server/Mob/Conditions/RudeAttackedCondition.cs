using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena <c>MSC_RUDEATTACKED</c> — fires when the mob's rude-attacked
/// counter has crossed the configured threshold
/// (<c>battle.mob_rudeattacked_count</c>, default 2). The counter is
/// owned by <see cref="MobEntity.RudeAttackedCount"/>; incrementing is
/// the responsibility of <see cref="IMobAiService.NotifyAttacked"/>.
/// </summary>
public sealed class RudeAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.RudeAttacked;

    /// <summary>rAthena <c>battle.mob_rudeattacked_count</c> default.</summary>
    public const int DefaultThreshold = 2;

    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
        => mob.RudeAttackedCount >= DefaultThreshold;
}
