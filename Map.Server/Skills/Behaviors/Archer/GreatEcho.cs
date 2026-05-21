using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_GREAT_ECHO — Minstrel/Wanderer Great Echo. Manual port of
/// <c>rathena-fork/src/map/skills/archer/greatecho.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 250 + 500*lv) + 50*WM_LESSON</c>; chorus
/// partner doubles the ratio. Splash + partner check TODO.</para>
/// </summary>
public sealed class GreatEcho : WeaponSkillImpl
{
    public GreatEcho() : base(SkillIds.WM_GREAT_ECHO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 250 + 500 * skillLevel);
    }
}
