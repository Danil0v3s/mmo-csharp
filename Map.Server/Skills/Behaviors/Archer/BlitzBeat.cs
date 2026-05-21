using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_BLITZBEAT — Hunter Blitz Beat. Manual port of
/// <c>rathena-fork/src/map/skills/archer/blitzbeat.cpp</c>.
/// Uses the base RecursiveDamageSplashSkillImpl pipeline — rathena-fork
/// declares no overrides for this skill.
/// </summary>
public sealed class BlitzBeat : RecursiveDamageSplashSkillImpl
{
    public BlitzBeat() : base(SkillIds.HT_BLITZBEAT) { }

    // No-op stub — rathena-fork class declares no overrides;
    // base class provides the standard pipeline.
}
