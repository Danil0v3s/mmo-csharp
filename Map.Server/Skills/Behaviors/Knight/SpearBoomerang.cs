using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_SPEARBOOMERANG — Knight Spear Boomerang. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/spearboomerang.cpp</c>.
///
/// Long-range thrown spear: single hit at (100 + 50 * lv)% ATK.
/// Range expands with skill level (range = 7 + (lv-1)).
/// </summary>
public sealed class SpearBoomerang : WeaponSkillImpl
{
    public SpearBoomerang() : base(SkillIds.KN_SPEARBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
