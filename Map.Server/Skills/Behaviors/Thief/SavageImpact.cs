using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_SAVAGE_IMPACT — Savage Impact. Manual port of
/// <c>rathena-fork/src/map/skills/thief/savageimpact.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 105*lv) + 5*pow</c>;
/// SC_SHADOW_EXCEED bonus +20*lv +2*pow (TODO).
/// </summary>
public sealed class SavageImpact : RecursiveDamageSplashSkillImpl
{
    public SavageImpact() : base(SkillIds.SHC_SAVAGE_IMPACT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 105 * skillLevel) + 5 * src.Stats.Pow;
}
