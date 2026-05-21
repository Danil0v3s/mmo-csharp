using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SKE_MIDNIGHT_KICK — Recursive splash; ratio +(-100 + 800 + 1500*lv) + 5*pow.</summary>
public sealed class MidnightKick : RecursiveDamageSplashSkillImpl
{
    public MidnightKick() : base(SkillIds.SKE_MIDNIGHT_KICK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 800 + 1500 * skillLevel) + 5 * src.Stats.Pow;
}
