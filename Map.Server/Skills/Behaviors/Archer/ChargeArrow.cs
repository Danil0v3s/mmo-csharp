using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_CHARGEARROW — Archer Charge Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/archer/chargearrow.cpp</c>.
/// Ratio: <c>+50 %</c>.
/// </summary>
public sealed class ChargeArrow : WeaponSkillImpl
{
    public ChargeArrow() : base(SkillIds.AC_CHARGEARROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50;
}
