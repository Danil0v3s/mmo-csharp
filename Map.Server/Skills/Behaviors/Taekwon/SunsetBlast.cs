using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_SUNSET_BLAST — Recursive splash; ratio +(-100 + 950 + 400*lv) + 5*pow.</summary>
public sealed class SunsetBlast : RecursiveDamageSplashSkillImpl
{
    public SunsetBlast() : base(SkillIds.SKE_SUNSET_BLAST) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 950 + 400 * skillLevel) + 5 * src.Stats.Pow;
}
