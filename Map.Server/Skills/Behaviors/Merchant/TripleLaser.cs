using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_TRIPLE_LASER — Meister Triple Laser. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/triplelaser.cpp</c>.
/// Ratio <c>+(-100 + 650 + 1150*lv) + 12*POW</c>.
/// </summary>
public sealed class TripleLaser : WeaponSkillImpl
{
    public TripleLaser() : base(SkillIds.MT_TRIPLE_LASER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 650 + 1150 * skillLevel) + 12 * src.Stats.Pow;
}
