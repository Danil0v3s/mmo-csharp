using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_JUPITEL_THUNDER_STORM — Hyper Novice Jupitel Thunderstorm.
/// Manual port of <c>rathena-fork/src/map/skills/novice/jupitelthunderstorm.cpp</c>.
/// Ratio <c>+(-100 + 1800*lv) + 3*SPL</c>. HN_SELFSTUDY_SOCERY amp +
/// SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class JupitelThunderstorm : RecursiveDamageSplashSkillImpl
{
    public JupitelThunderstorm() : base(SkillIds.HN_JUPITEL_THUNDER_STORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1800 * skillLevel) + 3 * src.Stats.Spl;
}
