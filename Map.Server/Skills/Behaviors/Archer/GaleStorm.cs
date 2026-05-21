using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_GALESTORM — Wind Hawk Gale Storm. Manual port of
/// <c>rathena-fork/src/map/skills/archer/galestorm.cpp</c>.
/// Ratio <c>+(-100 + 1350*lv) + 10*CON</c>; SC_CALAMITYGALE ×1.5 vs
/// Brute/Fish (caster SC TODO).
/// </summary>
public sealed class GaleStorm : RecursiveDamageSplashSkillImpl
{
    public GaleStorm() : base(SkillIds.WH_GALESTORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 1350 * skillLevel) + 10 * src.Stats.Con;
    }
}
