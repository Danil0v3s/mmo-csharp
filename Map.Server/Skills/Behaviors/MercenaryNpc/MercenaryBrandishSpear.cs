using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_BRANDISH — Mercenary Brandish Spear. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_brandishspear.cpp</c>.
/// Ratio <c>+(-100 + 100 + 20*lv)</c>; directional cone splash with
/// miscflag-tiered ratio boosts is TODO.
/// </summary>
public sealed class MercenaryBrandishSpear : WeaponSkillImpl
{
    public MercenaryBrandishSpear() : base(SkillIds.ML_BRANDISH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 + 20 * skillLevel);
}
