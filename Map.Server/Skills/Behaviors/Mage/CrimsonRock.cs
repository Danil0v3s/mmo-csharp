using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WL_CRIMSONROCK — Warlock Crimson Rock. Splash; ratio +(-100+700+600*lv).</summary>
public sealed class CrimsonRock : RecursiveDamageSplashSkillImpl
{
    public CrimsonRock() : base(SkillIds.WL_CRIMSONROCK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 700 + 600 * skillLevel);
}
