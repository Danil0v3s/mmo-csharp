using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_CLOSECONFINE — Close Confine. Manual port of
/// <c>rathena-fork/src/map/skills/thief/closeconfine.cpp</c>.
/// Applies SC_CLOSECONFINE with src.Id as val2 (linked confine).
/// </summary>
public sealed class CloseConfine : StatusSkillImpl
{
    public CloseConfine() : base(SkillIds.RG_CLOSECONFINE) { }
}
