using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_WHITE_TIGER — Recursive splash; ratio +(-100 + 400 + 1000*lv).</summary>
public sealed class TalismanOfWhiteTiger : RecursiveDamageSplashSkillImpl
{
    public TalismanOfWhiteTiger() : base(SkillIds.SOA_TALISMAN_OF_WHITE_TIGER) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 1000 * skillLevel);
}
