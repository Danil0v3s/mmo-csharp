using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_ARROWSTORM — Ranger Arrow Storm. Manual port of
/// <c>rathena-fork/src/map/skills/archer/arrowstorm.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 200 + 180*lv)</c> baseline; with
/// SC_FEARBREEZE active: <c>+(-100 + 200 + 250*lv)</c>. Caster SC
/// readback TODO.</para>
/// </summary>
public sealed class ArrowStorm : RecursiveDamageSplashSkillImpl
{
    public ArrowStorm() : base(SkillIds.RA_ARROWSTORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 200 + 180 * skillLevel);
    }
}
