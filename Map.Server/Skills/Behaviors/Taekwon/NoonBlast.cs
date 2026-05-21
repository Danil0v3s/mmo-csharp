using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_NOON_BLAST — Recursive splash; ratio +(-100 + 1500 + 1250*lv) + 5*pow.</summary>
public sealed class NoonBlast : RecursiveDamageSplashSkillImpl
{
    public NoonBlast() : base(SkillIds.SKE_NOON_BLAST) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 1250 * skillLevel) + 5 * src.Stats.Pow;
}
