using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_CIRCLE_OF_DIRECTIONS_AND_ELEMENTALS — Recursive splash; ratio +(-100 + 500 + 2000*lv) + 5*spl. Talisman/Soul partner bonuses TODO.</summary>
public sealed class CircleOfDirectionsAndElementals : RecursiveDamageSplashSkillImpl
{
    public CircleOfDirectionsAndElementals() : base(SkillIds.SOA_CIRCLE_OF_DIRECTIONS_AND_ELEMENTALS) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 + 2000 * skillLevel) + 5 * src.Stats.Spl;
}
