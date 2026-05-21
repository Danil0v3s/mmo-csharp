using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_SYURIKEN — Throw Shuriken. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/throwshuriken.cpp</c>.
/// Renewal: <c>+5*lv</c> ratio bump; pre-renewal flat.
/// </summary>
public sealed class ThrowShuriken : WeaponSkillImpl
{
    public ThrowShuriken() : base(SkillIds.NJ_SYURIKEN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 5 * skillLevel;
}
