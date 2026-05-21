namespace Map.Server.Skills.Units;

/// <summary>
/// Strategy table — <see cref="ISkillUnitTickHandler.SkillId"/> →
/// <see cref="ISkillUnitTickHandler"/>. Collects every registered
/// handler from DI and indexes by skill id; <see cref="SkillUnitService"/>
/// resolves through this rather than an inline per-skill switch.
///
/// Mirrors the dispatch table in <c>rathena-fork/src/map/skill.cpp</c>
/// where each ground unit type has a static handler row.
/// </summary>
public sealed class SkillUnitTickRegistry
{
    private readonly Dictionary<ushort, ISkillUnitTickHandler> _byId = new();

    public SkillUnitTickRegistry(IEnumerable<ISkillUnitTickHandler> handlers)
    {
        foreach (var h in handlers) _byId[h.SkillId] = h;
    }

    public ISkillUnitTickHandler? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);
    public int Count => _byId.Count;
    public IEnumerable<ushort> RegisteredSkillIds => _byId.Keys;
}
