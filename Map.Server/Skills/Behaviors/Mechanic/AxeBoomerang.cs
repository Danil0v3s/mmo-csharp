using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mechanic;

/// <summary>
/// NC_AXEBOOMERANG — Mechanic Axe Boomerang. Mirrors
/// <c>rathena-fork/src/map/skills/mechanic/axeboomerang.cpp</c>.
///
/// Single-target ranged physical at (250 + 50*lv)% ATK scaled by
/// weapon weight (weight table ports later). Requires axe equipped.
/// </summary>
public sealed class AxeBoomerang : WeaponSkillImpl
{
    public AxeBoomerang() : base(SkillIds.NC_AXEBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 150 + 50 * skillLevel;
}
