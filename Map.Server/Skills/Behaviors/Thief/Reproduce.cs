using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_REPRODUCE — Reproduce. Manual port of
/// <c>rathena-fork/src/map/skills/thief/reproduce.cpp</c>.
/// Toggles SC_REPRODUCE on target.
/// </summary>
public sealed class Reproduce : StatusSkillImpl
{
    public Reproduce() : base(SkillIds.SC_REPRODUCE) { }
}
