using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_SPEARBOOMERANG — Knight Spear Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/spearboomerang.cpp</c>.
/// Ratio <c>+50*lv</c>.
/// </summary>
public sealed class SpearBoomerang : WeaponSkillImpl
{
    public SpearBoomerang() : base(SkillIds.KN_SPEARBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
