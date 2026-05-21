using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_AXEBOOMERANG — Mechanic Axe Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/axeboomerang.cpp</c>.
/// Ratio <c>+(150 + 50*lv)</c>; weapon-weight bonus TODO.
/// </summary>
public sealed class AxeBoomerang : WeaponSkillImpl
{
    public AxeBoomerang() : base(SkillIds.NC_AXEBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 150 + 50 * skillLevel;
}
