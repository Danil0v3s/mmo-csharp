using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// OB_ZANGETSU — Distorted Crescent. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/distortedcrescent.cpp</c>.
/// Status buff (delegates to StatusSkillImpl).
/// </summary>
public sealed class DistortedCrescent : StatusSkillImpl
{
    public DistortedCrescent() : base(SkillIds.OB_ZANGETSU) { }
}
