using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_HUUMA — Throw Huuma Shuriken. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/throwhuumashuriken.cpp</c>.
/// Recursive splash; renewal <c>+(-150 + 250*lv)</c>, pre-renewal
/// <c>+(50 + 150*lv)</c>.
/// </summary>
public sealed class ThrowHuumaShuriken : RecursiveDamageSplashSkillImpl
{
    public ThrowHuumaShuriken() : base(SkillIds.NJ_HUUMA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-150 + 250 * skillLevel);
}
