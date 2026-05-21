using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_MAYHEMIC_THORNS — Biolo Mayhemic Thorns. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/mayhemicthorns.cpp</c>.
/// Ratio: <c>+(-100 + 200 + 300*lv) + 5*POW</c>.
/// </summary>
public sealed class MayhemicThorns : RecursiveDamageSplashSkillImpl
{
    public MayhemicThorns() : base(SkillIds.BO_MAYHEMIC_THORNS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 + 300 * skillLevel) + 5 * src.Stats.Pow;
}
