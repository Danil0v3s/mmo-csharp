using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_FEARBREEZE — Ranger Fear Breeze. Manual port of
/// <c>rathena-fork/src/map/skills/archer/fearbreeze.cpp</c>.
/// Self-buff SC_FEARBREEZE; base StatusSkillImpl drives the apply.
/// </summary>
public sealed class FearBreeze : StatusSkillImpl
{
    public FearBreeze() : base(SkillIds.RA_FEARBREEZE) { }
}
