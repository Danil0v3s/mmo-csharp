using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KAGEGISSEN — Shadow Flash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowflash.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 1500 + 950*lv) + 5*pow</c>.
/// SS_KAGENOMAI partner bonus + alt-flag scaling are TODO.
/// </summary>
public sealed class ShadowFlash : RecursiveDamageSplashSkillImpl
{
    public ShadowFlash() : base(SkillIds.SS_KAGEGISSEN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 950 * skillLevel) + 5 * src.Stats.Pow;
}
