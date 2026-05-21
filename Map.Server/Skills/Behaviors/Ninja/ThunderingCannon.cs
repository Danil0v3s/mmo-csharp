using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_RAIDENPOU — Thundering Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/thunderingcannon.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 600 + 1100*lv) + 5*spl</c>.
/// SS_ANTENPOU partner bonus + SC_WIND_CHARM_POWER +8500 are TODO.
/// </summary>
public sealed class ThunderingCannon : RecursiveDamageSplashSkillImpl
{
    public ThunderingCannon() : base(SkillIds.SS_RAIDENPOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 + 1100 * skillLevel) + 5 * src.Stats.Spl;
}
