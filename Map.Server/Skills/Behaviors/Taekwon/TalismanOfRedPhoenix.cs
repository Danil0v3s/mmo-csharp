using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_RED_PHOENIX — Recursive splash; ratio +(-100 + 1400 + 1450*lv).</summary>
public sealed class TalismanOfRedPhoenix : RecursiveDamageSplashSkillImpl
{
    public TalismanOfRedPhoenix() : base(SkillIds.SOA_TALISMAN_OF_RED_PHOENIX) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1400 + 1450 * skillLevel);
}
