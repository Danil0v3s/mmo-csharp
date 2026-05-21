using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ST_CHASEWALK — Stealth. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stealth.cpp</c>.
/// Toggles SC_CHASEWALK on the target.
/// </summary>
public sealed class Stealth : StatusSkillImpl
{
    public Stealth() : base(SkillIds.ST_CHASEWALK) { }
}
