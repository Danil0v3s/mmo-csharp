namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Strategy dispatch table — <see cref="SkillDamageKind"/> →
/// <see cref="ISkillResolver"/>. Collects all registered resolvers
/// from DI and indexes them by their <see cref="ISkillResolver.Kind"/>.
///
/// Replaces the prior switch-on-DamageKind in <c>SkillCastService</c>;
/// matches the SkillUnitService.SpecFor / ItemEffectRegistry pattern.
/// </summary>
public sealed class SkillResolverRegistry
{
    private readonly Dictionary<SkillDamageKind, ISkillResolver> _byKind = new();

    public SkillResolverRegistry(IEnumerable<ISkillResolver> resolvers)
    {
        foreach (var r in resolvers) _byKind[r.Kind] = r;
    }

    public ISkillResolver? Get(SkillDamageKind kind) => _byKind.GetValueOrDefault(kind);
}
