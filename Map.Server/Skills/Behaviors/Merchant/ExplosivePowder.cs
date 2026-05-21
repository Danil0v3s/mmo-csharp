using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_EXPLOSIVE_POWDER — Biolo Explosive Powder. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/explosivepowder.cpp</c>.
/// Ratio: <c>+(-100 + 500 + 650*lv) + 5*POW</c> (SC_RESEARCHREPORT
/// +100*lv bonus TODO).
/// </summary>
public sealed class ExplosivePowder : RecursiveDamageSplashSkillImpl
{
    public ExplosivePowder() : base(SkillIds.BO_EXPLOSIVE_POWDER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 + 650 * skillLevel) + 5 * src.Stats.Pow;
}
