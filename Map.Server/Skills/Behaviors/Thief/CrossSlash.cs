using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_CROSS_SLASH — Cross Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossslash.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 300*lv) + 5*pow</c>.
/// SC_SHADOW_EXCEED bonus (+60*lv +2*pow) is TODO.
/// </summary>
public sealed class CrossSlash : RecursiveDamageSplashSkillImpl
{
    public CrossSlash() : base(SkillIds.SHC_CROSS_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 * skillLevel) + 5 * src.Stats.Pow;
}
