using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_ARROWVULCAN — Clown/Gypsy Vulcan Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/archer/vulcanarrow.cpp</c>.
/// Renewal: <c>+(400 + 100*lv)</c>.
/// </summary>
public sealed class VulcanArrow : WeaponSkillImpl
{
    public VulcanArrow() : base(SkillIds.CG_ARROWVULCAN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400 + 100 * skillLevel;
}
