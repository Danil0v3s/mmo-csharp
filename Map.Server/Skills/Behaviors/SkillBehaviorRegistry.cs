namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Strategy table — <see cref="ISkillBehavior.SkillId"/> →
/// <see cref="ISkillBehavior"/>. Collects all registered plugins from
/// DI and indexes them by their rAthena skill id.
///
/// Mirrors <see cref="Resolvers.SkillResolverRegistry"/> but keyed on
/// skill id rather than <see cref="SkillDamageKind"/> — the layer
/// above the generic dispatch.
/// </summary>
public sealed class SkillBehaviorRegistry
{
    private readonly Dictionary<ushort, ISkillBehavior> _byId = new();

    public SkillBehaviorRegistry(IEnumerable<ISkillBehavior> behaviors)
    {
        foreach (var b in behaviors) _byId[b.SkillId] = b;
    }

    public ISkillBehavior? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);
    public int Count => _byId.Count;
}
