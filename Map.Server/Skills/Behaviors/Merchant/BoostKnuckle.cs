using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_BOOSTKNUCKLE — Mechanic Boost Knuckle. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/boostknuckle.cpp</c>.
/// Ratio: <c>+(-100 + 260*lv) + DEX</c>.
/// </summary>
public sealed class BoostKnuckle : WeaponSkillImpl
{
    public BoostKnuckle() : base(SkillIds.NC_BOOSTKNUCKLE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 260 * skillLevel) + src.Stats.Dex;
}
