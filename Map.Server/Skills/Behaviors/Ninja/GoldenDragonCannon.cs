using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KINRYUUHOU — Golden Dragon Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/goldendragoncannon.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 800 + 1500*lv) + 5*spl</c>.
/// SS_ANTENPOU bonus and SC_GROUND_CHARM_POWER +5500 are TODO.
/// </summary>
public sealed class GoldenDragonCannon : RecursiveDamageSplashSkillImpl
{
    public GoldenDragonCannon() : base(SkillIds.SS_KINRYUUHOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 800 + 1500 * skillLevel) + 5 * src.Stats.Spl;
}
