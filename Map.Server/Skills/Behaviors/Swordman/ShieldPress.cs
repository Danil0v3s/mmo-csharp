using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_SHIELDPRESS — Royal Guard Shield Press. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldpress.cpp</c>.
/// Ratio <c>+(-100 + 200*lv) + base STR</c>. Shield mastery + weight
/// bonus are TODO.
/// </summary>
public sealed class ShieldPress : WeaponSkillImpl
{
    public ShieldPress() : base(SkillIds.LG_SHIELDPRESS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel) + src.Stats.Str;
}
