using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_SPEARSTAB — Knight Spear Stab. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/spearstab.cpp</c>.
///
/// Single ranged physical at (100 + 20 * lv)% ATK + knockback. The
/// knockback line ports separately (directional-movement helper);
/// damage flows through the standard weapon pipeline.
/// </summary>
public sealed class SpearStab : WeaponSkillImpl
{
    public SpearStab() : base(SkillIds.KN_SPEARSTAB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;
}
