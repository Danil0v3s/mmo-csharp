using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_JUPITEL_THUNDER_STORM — Hyper Novice Jupitel Thunderstorm. Port
/// of <c>rathena-fork/src/map/skills/novice/jupitelthunderstorm.cpp</c>.
///
/// Ratio: <c>-100 + 1800·lv + 3·SPL</c>.
/// Mastery: <c>+ HN_SELFSTUDY_SOCERY · 3 · lv</c> then SOCERY%.
/// SC_RULEBREAK: <c>· (100 + 70) / 100</c>.
/// </summary>
public sealed class JupitelThunderstorm : RecursiveDamageSplashSkillImpl
{
    public JupitelThunderstorm() : base(SkillIds.HN_JUPITEL_THUNDER_STORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 1800 * skillLevel) + 3 * src.Stats.Spl;
        ratio = HyperNoviceFormulas.ApplySoceryBoost(ratio, src, skillLevel, perLevel: 3, ctx);
        ratio = HyperNoviceFormulas.ApplyRuleBreakBoost(ratio, src, pct: 70, ctx);
        return ratio;
    }
}
