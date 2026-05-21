using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_DAWN_BREAK — Recursive splash; ratio +(-100 + 600 + 700*lv) + 5*pow. SKE_SKY_MASTERY partner bonus TODO.</summary>
public sealed class DawnBreak : RecursiveDamageSplashSkillImpl
{
    public DawnBreak() : base(SkillIds.SKE_DAWN_BREAK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 + 700 * skillLevel) + 5 * src.Stats.Pow;
}
