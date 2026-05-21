using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KG_KYOMU — Empty Shadow. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/emptyshadow.cpp</c>.
/// Status-only buff (delegates to StatusSkillImpl).
/// </summary>
public sealed class EmptyShadow : StatusSkillImpl
{
    public EmptyShadow() : base(SkillIds.KG_KYOMU) { }
}
