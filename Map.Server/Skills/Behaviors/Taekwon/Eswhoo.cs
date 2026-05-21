using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SWHOO — Eswhoo. Recursive splash; ratio +(1000 + 200*lv).</summary>
public sealed class Eswhoo : RecursiveDamageSplashSkillImpl
{
    public Eswhoo() : base(SkillIds.SP_SWHOO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 1000 + 200 * skillLevel;
}
