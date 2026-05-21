using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_SOLARBURST — Recursive splash; ratio +(900 + 220*lv).</summary>
public sealed class SolarBurst : RecursiveDamageSplashSkillImpl
{
    public SolarBurst() : base(SkillIds.SJ_SOLARBURST) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900 + 220 * skillLevel;
}
