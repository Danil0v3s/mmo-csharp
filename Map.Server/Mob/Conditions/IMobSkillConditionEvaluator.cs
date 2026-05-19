using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// Strategy interface for evaluating a <see cref="MobSkillCondition"/>.
/// One implementation per condition type — registered in
/// <see cref="MobSkillConditionRegistry"/>.
///
/// Same shape as <see cref="Skills.Resolvers.ISkillResolver"/> — adding
/// a new mob_skill_db condition (e.g. <c>MASTERATTACKED</c>) ships a
/// new evaluator class instead of a new switch arm.
/// </summary>
public interface IMobSkillConditionEvaluator
{
    MobSkillCondition Kind { get; }
    bool IsMet(MobEntity mob, Entity? target, MobSkillEntry entry);
}
