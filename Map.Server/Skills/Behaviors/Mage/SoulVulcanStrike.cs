using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_SOUL_VC_STRIKE — Arch Mage Soul Vulcan Strike. Splash magic; ratio +(-100 + 300*lv + 3*SPL).</summary>
public sealed class SoulVulcanStrike : RecursiveDamageSplashSkillImpl
{
    public SoulVulcanStrike() : base(SkillIds.AG_SOUL_VC_STRIKE) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 * skillLevel) + 3 * src.Stats.Spl;
}
