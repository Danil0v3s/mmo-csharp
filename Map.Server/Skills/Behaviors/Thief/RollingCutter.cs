using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_ROLLINGCUTTER — Rolling Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/thief/rollingcutter.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 50 + 80*lv)</c>. Each cast
/// stacks SC_ROLLINGCUTTER up to 10 (TODO).
/// </summary>
public sealed class RollingCutter : RecursiveDamageSplashSkillImpl
{
    public RollingCutter() : base(SkillIds.GC_ROLLINGCUTTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 + 80 * skillLevel);
}
