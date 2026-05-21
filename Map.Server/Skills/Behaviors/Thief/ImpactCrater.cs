using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_IMPACT_CRATER — Impact Crater. Manual port of
/// <c>rathena-fork/src/map/skills/thief/impactcrater.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 80*lv) + 5*pow</c>. Hit count
/// scales with SC_ROLLINGCUTTER val1 (TODO).
/// </summary>
public sealed class ImpactCrater : RecursiveDamageSplashSkillImpl
{
    public ImpactCrater() : base(SkillIds.SHC_IMPACT_CRATER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 * skillLevel) + 5 * src.Stats.Pow;
}
