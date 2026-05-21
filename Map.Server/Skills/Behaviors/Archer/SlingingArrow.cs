using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_THROWARROW — Dancer Slinging Arrow (Throw Arrow). Manual port
/// of <c>rathena-fork/src/map/skills/archer/slingingarrow.cpp</c>.
/// Renewal ratio: <c>+(10 + 40*lv)</c>.
/// </summary>
public sealed class SlingingArrow : WeaponSkillImpl
{
    public SlingingArrow() : base(SkillIds.DC_THROWARROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 + 40 * skillLevel;
}
