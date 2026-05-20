namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Strategy table — <see cref="SkillImpl.SkillId"/> →
/// <see cref="SkillImpl"/>. Collects all registered plugins from
/// DI and indexes them by their rAthena skill id.
///
/// Mirrors <c>rathena-fork/src/map/skills/skill_factory.cpp</c>:
/// each <see cref="SkillImpl"/> subclass is constructed once and
/// looked up by id at cast time.
/// </summary>
public sealed class SkillBehaviorRegistry
{
    private readonly Dictionary<ushort, SkillImpl> _byId = new();

    public SkillBehaviorRegistry(IEnumerable<SkillImpl> impls)
    {
        foreach (var s in impls) _byId[s.SkillId] = s;
    }

    public SkillImpl? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);
    public int Count => _byId.Count;
}
