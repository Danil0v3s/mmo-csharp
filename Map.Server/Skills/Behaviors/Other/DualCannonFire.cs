using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_DUAL_CANNON_FIRE — ABR Dual Cannon Fire. Manual port of
/// <c>rathena-fork/src/map/skills/other/dualcannonfire.cpp</c>.
/// Ratio <c>+(-100 + 8000)</c>.
/// </summary>
public sealed class DualCannonFire : WeaponSkillImpl
{
    public DualCannonFire() : base(SkillIds.ABR_DUAL_CANNON_FIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 8000);
}
