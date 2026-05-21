using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_BRANDISHSPEAR — Knight Brandish Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/brandishspear.cpp</c>.
/// Renewal ratio <c>+(-100 + 400 + 100*lv) + 3*STR</c>. Targeted
/// directional cone splash (map_foreachindir) is TODO — for now we
/// land a single hit at the named target through the standard
/// WeaponSkillImpl flow.
/// </summary>
public sealed class BrandishSpear : WeaponSkillImpl
{
    public BrandishSpear() : base(SkillIds.KN_BRANDISHSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 100 * skillLevel) + 3 * src.Stats.Str;
}
