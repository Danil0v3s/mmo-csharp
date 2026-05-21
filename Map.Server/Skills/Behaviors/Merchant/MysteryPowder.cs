using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_MYSTERY_POWDER — Biolo Mystery Powder. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/mysterypowder.cpp</c>.
/// Ratio: <c>+(-100 + 1500 + 4000*lv) + 5*POW</c>.
/// </summary>
public sealed class MysteryPowder : RecursiveDamageSplashSkillImpl
{
    public MysteryPowder() : base(SkillIds.BO_MYSTERY_POWDER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 4000 * skillLevel) + 5 * src.Stats.Pow;
}
