namespace Map.Server.Mob.Conditions;

/// <summary>
/// <see cref="MobSkillCondition"/> → <see cref="IMobSkillConditionEvaluator"/>
/// dispatch table. Same strategy shape as SkillResolverRegistry /
/// ActionRegistry / ItemEffectRegistry.
/// </summary>
public sealed class MobSkillConditionRegistry
{
    private readonly Dictionary<MobSkillCondition, IMobSkillConditionEvaluator> _byKind = new();

    public MobSkillConditionRegistry(IEnumerable<IMobSkillConditionEvaluator> evaluators)
    {
        foreach (var e in evaluators) _byKind[e.Kind] = e;
    }

    public IMobSkillConditionEvaluator? Get(MobSkillCondition kind) => _byKind.GetValueOrDefault(kind);
}
