using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_RUSH_STRIKE — Meister Rush Strike. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/rushstrike.cpp</c>.
/// Ratio <c>+(-100 + 3500*lv) + 5*POW</c>.
/// </summary>
public sealed class RushStrike : RecursiveDamageSplashSkillImpl
{
    public RushStrike() : base(SkillIds.MT_RUSH_STRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 3500 * skillLevel) + 5 * src.Stats.Pow;
}
